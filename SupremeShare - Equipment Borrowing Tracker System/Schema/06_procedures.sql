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
AS $$
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
$$;

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
LANGUAGE plpgsql AS $$
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
$$;

-- 3. sp_process_return_transaction
-- Uses VARCHAR condition ('Good' or 'Damaged')
CREATE OR REPLACE PROCEDURE sp_process_return_transaction(
    p_borrow_id INT,
    p_return_condition VARCHAR(100)
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_eunit_id INT;
    v_equipment_id INT;
    v_new_status VARCHAR(50);
BEGIN
    -- 1. Get the Unit ID and Equipment ID from the borrow record
    SELECT b.eunit_id, u.equipment_id
    INTO v_eunit_id, v_equipment_id
    FROM borrow_record b
    JOIN equipment_unit u ON b.eunit_id = u.eunit_id
    WHERE b.borrow_id = p_borrow_id;

    -- 2. Determine new unit status based on condition
    IF p_return_condition = 'Good' THEN
        v_new_status := 'available';
    ELSE
        v_new_status := 'for review';
    END IF;

    -- 3. Update BORROW_RECORD
    UPDATE borrow_record
    SET borrow_date_returned = NOW(),
        borrow_return_condition = (p_return_condition = 'Good'),
        borrow_status = 'returned',
        borrow_last_updated = NOW()
    WHERE borrow_id = p_borrow_id;

    -- 4. Update EQUIPMENT_UNIT status
    UPDATE equipment_unit
    SET eunit_status = v_new_status,
        eunit_condition = (p_return_condition = 'Good'),
        eunit_last_updated = NOW()
    WHERE eunit_id = v_eunit_id;

    -- 5. Update EQUIPMENT total quantity (only if available)
    IF v_new_status = 'available' THEN
        UPDATE equipment
        SET equipment_quantity = equipment_quantity + 1, equipment_last_updated = NOW()
        WHERE equipment_id = v_equipment_id;
    END IF;

EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'Return processing failed: %', SQLERRM;
        RAISE;
END;
$$;

-- 4. sp_process_return_boolean
-- For C# app using boolean condition
CREATE OR REPLACE PROCEDURE sp_process_return_boolean(
    p_borrow_id INT, 
    p_return_condition BOOLEAN,
    p_user_id INT
)
LANGUAGE plpgsql AS $$
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
        v_new_status := 'maintenance'; 
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
    IF p_return_condition THEN
        UPDATE equipment SET equipment_quantity = equipment_quantity + 1, equipment_last_updated = NOW()
        WHERE equipment_id = v_equipment_id;
    END IF;

EXCEPTION WHEN OTHERS THEN 
    RAISE;
END;
$$;

-- 5. sp_update_overdue_statuses
-- Updates all borrowed items past due date to 'overdue'
CREATE OR REPLACE PROCEDURE sp_update_overdue_statuses()
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE borrow_record
    SET borrow_status = 'overdue', borrow_last_updated = NOW()
    WHERE borrow_status = 'borrowed' AND borrow_due_date < NOW();

EXCEPTION
    WHEN OTHERS THEN
        RAISE;
END;
$$;

-- 6. sp_mark_as_lost
-- Marks a borrow record as lost (triggers trg_enforce_lost_protocol)
CREATE OR REPLACE PROCEDURE sp_mark_as_lost(
    p_borrow_id INT,
    p_user_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE borrow_record 
    SET borrow_status = 'lost', borrow_last_updated = NOW()
    WHERE borrow_id = p_borrow_id;
    -- Trigger trg_enforce_lost_protocol will handle blocking student and updating unit status
END;
$$;
