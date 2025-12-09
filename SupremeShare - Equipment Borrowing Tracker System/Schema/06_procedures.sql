-- ============================================
-- Stored Procedures
-- ============================================

-- 1. sp_process_borrow_transaction
-- Auto-selects an available unit by equipment_id
CREATE OR REPLACE PROCEDURE sp_process_borrow_transaction(
    p_student_id INT,
    p_user_id INT,
    p_equipment_id INT,
    p_due_date TIMESTAMP
)
LANGUAGE plpgsql
AS $
DECLARE
    v_transaction_id INT;
    v_eunit_id INT;
    v_trans_code VARCHAR(100);
BEGIN
    -- 1. Find the first available unit for this equipment type
    SELECT eunit_id INTO v_eunit_id
    FROM equipment_unit
    WHERE equipment_id = p_equipment_id AND eunit_status = 'available'
    LIMIT 1;

    IF v_eunit_id IS NULL THEN
        RAISE EXCEPTION 'No available units for this equipment ID';
    END IF;

    -- 2. Generate a Transaction Code (e.g., TR-YYYYMMDD-Random)
    v_trans_code := 'TR-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || LPAD(FLOOR(RANDOM() * 1000)::TEXT, 3, '0');

    -- 3. Insert into TRANSACTION table
    INSERT INTO transaction (student_id, user_id, transaction_code, transaction_date_borrowed)
    VALUES (p_student_id, p_user_id, v_trans_code, NOW())
    RETURNING transaction_id INTO v_transaction_id;

    -- 4. Insert into BORROW_RECORD table
    INSERT INTO borrow_record (transaction_id, eunit_id, borrow_due_date, borrow_status)
    VALUES (v_transaction_id, v_eunit_id, p_due_date, 'borrowed');

    -- 5. Update EQUIPMENT_UNIT status
    UPDATE equipment_unit
    SET eunit_status = 'borrowed', eunit_last_updated = NOW()
    WHERE eunit_id = v_eunit_id;

    -- 6. Update EQUIPMENT total quantity (decrement stock)
    UPDATE equipment
    SET equipment_quantity = equipment_quantity - 1, equipment_last_updated = NOW()
    WHERE equipment_id = p_equipment_id;

EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'Transaction failed: %', SQLERRM;
        RAISE;
END;
$;

-- 2. sp_process_borrow_by_unit
-- For C# app that selects specific unit
CREATE OR REPLACE PROCEDURE sp_process_borrow_by_unit(
    p_student_id INT, 
    p_user_id INT, 
    p_eunit_id INT, 
    p_due_date TIMESTAMP,
    OUT p_transaction_id INT,
    OUT p_transaction_code VARCHAR
)
LANGUAGE plpgsql AS $
DECLARE
    v_equipment_id INT;
BEGIN
    -- Verify unit is available
    SELECT equipment_id INTO v_equipment_id FROM equipment_unit
    WHERE eunit_id = p_eunit_id AND eunit_status = 'available';
    
    IF v_equipment_id IS NULL THEN 
        RAISE EXCEPTION 'Unit is not available for borrowing'; 
    END IF;

    -- Generate transaction code
    p_transaction_code := 'TR-' || TO_CHAR(NOW(), 'YYYYMMDD-HH24MISS') || '-' || LPAD(FLOOR(RANDOM() * 1000)::TEXT, 3, '0');

    -- Create transaction
    INSERT INTO transaction (student_id, user_id, transaction_code, transaction_date_borrowed)
    VALUES (p_student_id, p_user_id, p_transaction_code, NOW())
    RETURNING transaction_id INTO p_transaction_id;

    -- Create borrow record
    INSERT INTO borrow_record (transaction_id, eunit_id, borrow_due_date, borrow_status)
    VALUES (p_transaction_id, p_eunit_id, p_due_date, 'borrowed');

    -- Update unit status
    UPDATE equipment_unit SET eunit_status = 'borrowed', eunit_last_updated = NOW() 
    WHERE eunit_id = p_eunit_id;

    -- Decrement equipment quantity
    UPDATE equipment SET equipment_quantity = equipment_quantity - 1, equipment_last_updated = NOW()
    WHERE equipment_id = v_equipment_id;

EXCEPTION WHEN OTHERS THEN 
    RAISE;
END;
$;

-- 3. sp_process_return_boolean
-- For C# app using boolean condition
-- Damaged items (FALSE) go to 'for review' status per group agreement
CREATE OR REPLACE PROCEDURE sp_process_return_boolean(
    p_borrow_id INT, 
    p_return_condition BOOLEAN,
    p_user_id INT
)
LANGUAGE plpgsql AS $
DECLARE
    v_eunit_id INT; 
    v_equipment_id INT; 
    v_new_status VARCHAR(50);
BEGIN
    -- Get unit and equipment info
    SELECT b.eunit_id, u.equipment_id INTO v_eunit_id, v_equipment_id
    FROM borrow_record b 
    JOIN equipment_unit u ON b.eunit_id = u.eunit_id
    WHERE b.borrow_id = p_borrow_id AND b.borrow_status IN ('borrowed', 'overdue');

    IF v_eunit_id IS NULL THEN
        RAISE EXCEPTION 'Borrow record not found or already returned';
    END IF;

    -- Determine new status based on condition
    IF p_return_condition THEN 
        v_new_status := 'available';
    ELSE 
        -- Changed from 'maintenance' to 'for review' per group agreement
        v_new_status := 'for review'; 
    END IF;

    -- Update borrow record
    UPDATE borrow_record 
    SET borrow_date_returned = NOW(), 
        borrow_return_condition = p_return_condition, 
        borrow_status = 'returned',
        borrow_last_updated = NOW()
    WHERE borrow_id = p_borrow_id;

    -- Update equipment unit
    UPDATE equipment_unit 
    SET eunit_status = v_new_status, 
        eunit_condition = p_return_condition,
        eunit_last_updated = NOW()
    WHERE eunit_id = v_eunit_id;

    -- Increment equipment quantity only if returned in good condition
    -- Items 'for review' are not counted as available stock
    IF p_return_condition THEN
        UPDATE equipment SET equipment_quantity = equipment_quantity + 1, equipment_last_updated = NOW()
        WHERE equipment_id = v_equipment_id;
    END IF;

EXCEPTION WHEN OTHERS THEN 
    RAISE;
END;
$;

-- 4. sp_update_overdue_statuses
-- Updates all borrowed items past due date to 'overdue'
CREATE OR REPLACE PROCEDURE sp_update_overdue_statuses()
LANGUAGE plpgsql
AS $
BEGIN
    UPDATE borrow_record
    SET borrow_status = 'overdue', borrow_last_updated = NOW()
    WHERE borrow_status = 'borrowed' AND borrow_due_date < NOW();

EXCEPTION
    WHEN OTHERS THEN
        RAISE;
END;
$;

-- 5. sp_mark_as_lost
-- Marks a borrow record as lost (triggers trg_enforce_lost_protocol)
CREATE OR REPLACE PROCEDURE sp_mark_as_lost(
    p_borrow_id INT,
    p_user_id INT
)
LANGUAGE plpgsql AS $
BEGIN
    UPDATE borrow_record 
    SET borrow_status = 'lost', borrow_last_updated = NOW()
    WHERE borrow_id = p_borrow_id;
    -- Trigger trg_enforce_lost_protocol will handle blocking student and updating unit status
END;
$;


-- 6. sp_update_unit_condition
-- Updates unit condition and automatically adjusts status (UPDATED for 'For Review' workflow)
-- If condition is Good and status is maintenance/for review/lost -> set to available
-- If condition is Damaged and status is available -> set to maintenance
-- If condition is Damaged and status is 'for review' -> set to maintenance
CREATE OR REPLACE PROCEDURE sp_update_unit_condition(
    p_unit_id INT,
    p_condition BOOLEAN,
    p_user_id INT
)
LANGUAGE plpgsql AS $
DECLARE
    v_old_condition BOOLEAN;
    v_old_status VARCHAR(50);
    v_new_status VARCHAR(50);
BEGIN
    -- Get current unit state
    SELECT eunit_condition, eunit_status 
    INTO v_old_condition, v_old_status
    FROM equipment_unit 
    WHERE eunit_id = p_unit_id;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Unit not found';
    END IF;
    
    -- LOGIC UPDATE BASED ON CHAT:
    -- 1. If we are fixing it (TRUE) and it was broken (Maintenance/For Review/Lost), make it Available.
    IF p_condition = TRUE AND v_old_status IN ('maintenance', 'for review', 'lost') THEN
        v_new_status := 'available';

    -- 2. If we mark it Damaged (FALSE) and it was Available, send to Maintenance.
    ELSIF p_condition = FALSE AND v_old_status = 'available' THEN
        v_new_status := 'maintenance';

    -- 3. [NEW] If it is 'For Review' and we confirm it is Damaged (FALSE), move to Maintenance.
    ELSIF p_condition = FALSE AND v_old_status = 'for review' THEN
        v_new_status := 'maintenance';
        
    ELSE
        v_new_status := v_old_status;
    END IF;
    
    -- Update the unit
    UPDATE equipment_unit 
    SET eunit_condition = p_condition,
        eunit_status = v_new_status,
        eunit_last_updated = NOW()
    WHERE eunit_id = p_unit_id;
    
    -- Log the change
    INSERT INTO audit_log (user_id, audit_action, audit_old_data, audit_new_data, audit_logged_at)
    VALUES (
        p_user_id,
        'Unit condition updated for ID ' || p_unit_id,
        'Condition: ' || CASE WHEN v_old_condition THEN 'Good' ELSE 'Damaged' END || ', Status: ' || v_old_status,
        'Condition: ' || CASE WHEN p_condition THEN 'Good' ELSE 'Damaged' END || ', Status: ' || v_new_status,
        NOW()
    );
    
EXCEPTION WHEN OTHERS THEN 
    RAISE;
END;
$;
