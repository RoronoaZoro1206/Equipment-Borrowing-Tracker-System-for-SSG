-- ============================================
-- Migration: Create Stored Procedures & Functions
-- Date: 2025-11-30
-- Updated: 2025-12-09 (Removed sp_process_return_transaction - unused legacy procedure)
-- Description: Transaction processing stored routines
-- Based on: 05_functions.sql, 06_procedures.sql, and md_files documentation
-- ============================================

-- =============================================
-- FUNCTIONS (from 05_functions.sql)
-- =============================================

-- 1. fn_get_available_stock
CREATE OR REPLACE FUNCTION fn_get_available_stock(p_equipment_id INT)
RETURNS INT AS $
DECLARE
    v_available_count INT;
BEGIN
    SELECT COUNT(*) INTO v_available_count
    FROM equipment_unit
    WHERE equipment_id = p_equipment_id 
      AND eunit_status = 'available';
    RETURN COALESCE(v_available_count, 0);
END;
$ LANGUAGE plpgsql;

-- 2. fn_count_borrow_status
CREATE OR REPLACE FUNCTION fn_count_borrow_status(p_status_type VARCHAR)
RETURNS INT AS $
DECLARE
    v_count INT;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM borrow_record
    WHERE borrow_status = p_status_type;
    RETURN COALESCE(v_count, 0);
END;
$ LANGUAGE plpgsql;

-- 3. fn_check_student_eligibility
CREATE OR REPLACE FUNCTION fn_check_student_eligibility(p_student_id INT)
RETURNS BOOLEAN AS $
DECLARE
    v_status BOOLEAN;
BEGIN
    SELECT student_status INTO v_status FROM student WHERE student_id = p_student_id;
    RETURN COALESCE(v_status, FALSE);
END;
$ LANGUAGE plpgsql;

-- 4. fn_calculate_days_overdue (additional helper)
CREATE OR REPLACE FUNCTION fn_calculate_days_overdue(p_borrow_id INT)
RETURNS INT AS $
DECLARE
    v_due_date TIMESTAMP;
    v_days INT;
BEGIN
    SELECT borrow_due_date INTO v_due_date FROM borrow_record WHERE borrow_id = p_borrow_id;
    IF v_due_date IS NULL THEN RETURN 0; END IF;
    v_days := EXTRACT(DAY FROM (NOW() - v_due_date))::INT;
    RETURN GREATEST(v_days, 0);
END;
$ LANGUAGE plpgsql;

-- =============================================
-- TRIGGERS (from 05_functions.sql)
-- =============================================

-- 5. Trigger: Enforce Lost Protocol
CREATE OR REPLACE FUNCTION fn_trigger_enforce_lost()
RETURNS TRIGGER AS $
DECLARE
    v_student_id INT;
BEGIN
    IF NEW.borrow_status = 'lost' AND OLD.borrow_status != 'lost' THEN
        -- Update equipment unit status to 'lost'
        UPDATE equipment_unit SET eunit_status = 'lost' WHERE eunit_id = NEW.eunit_id;
        -- Find and block the student
        SELECT student_id INTO v_student_id FROM transaction WHERE transaction_id = NEW.transaction_id;
        UPDATE student SET student_status = FALSE WHERE student_id = v_student_id;
    END IF;
    RETURN NEW;
END;
$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_enforce_lost_protocol ON borrow_record;
CREATE TRIGGER trg_enforce_lost_protocol
AFTER UPDATE ON borrow_record
FOR EACH ROW EXECUTE FUNCTION fn_trigger_enforce_lost();

-- 6. Trigger: Audit Borrow Status Changes
CREATE OR REPLACE FUNCTION fn_trigger_audit_borrow_status()
RETURNS TRIGGER AS $
DECLARE
    v_current_user_id INT := 1; -- Default system admin
BEGIN
    IF NEW.borrow_status IS DISTINCT FROM OLD.borrow_status THEN
        INSERT INTO audit_log (user_id, audit_action, audit_old_data, audit_new_data, audit_logged_at)
        VALUES (v_current_user_id, 'Borrow Status Changed for Record ID ' || NEW.borrow_id, 
                OLD.borrow_status, NEW.borrow_status, NOW());
    END IF;
    RETURN NEW;
END;
$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_audit_borrow_status_change ON borrow_record;
CREATE TRIGGER trg_audit_borrow_status_change
AFTER UPDATE ON borrow_record
FOR EACH ROW EXECUTE FUNCTION fn_trigger_audit_borrow_status();

-- 7. Trigger: Log Timeline Activity
CREATE OR REPLACE FUNCTION fn_log_timeline_activity()
RETURNS TRIGGER AS $
DECLARE
    v_action_description VARCHAR(255);
    v_current_user_id INT;
BEGIN
    IF NEW.borrow_status IS DISTINCT FROM OLD.borrow_status THEN
        IF NEW.borrow_status = 'returned' THEN
            v_action_description := 'Item unit ' || NEW.eunit_id || ' returned.';
        ELSIF NEW.borrow_status = 'lost' THEN
            v_action_description := 'Item unit ' || NEW.eunit_id || ' marked as Lost/Missing.';
        ELSIF NEW.borrow_status = 'overdue' THEN
            v_action_description := 'Item unit ' || NEW.eunit_id || ' marked as Overdue.';
        ELSE
            v_action_description := 'Status changed to ' || NEW.borrow_status;
        END IF;

        BEGIN
            v_current_user_id := current_setting('app.current_user_id')::INT;
        EXCEPTION WHEN OTHERS THEN
            SELECT user_id INTO v_current_user_id FROM transaction WHERE transaction_id = NEW.transaction_id;
        END;

        INSERT INTO audit_log (user_id, audit_action, audit_old_data, audit_new_data, audit_logged_at)
        VALUES (v_current_user_id, v_action_description, 'Status: ' || OLD.borrow_status, 
                'Status: ' || NEW.borrow_status, NOW());
    END IF;
    RETURN NEW;
END;
$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_log_timeline_activity ON borrow_record;
CREATE TRIGGER trg_log_timeline_activity
AFTER UPDATE ON borrow_record
FOR EACH ROW EXECUTE FUNCTION fn_log_timeline_activity();

-- =============================================
-- STORED PROCEDURES (from 06_procedures.sql)
-- =============================================

-- 8. sp_process_borrow_transaction (Original from docs)
-- Note: This version auto-selects an available unit by equipment_id
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
    -- Find the first available unit for this equipment type
    SELECT eunit_id INTO v_eunit_id
    FROM equipment_unit
    WHERE equipment_id = p_equipment_id AND eunit_status = 'available'
    LIMIT 1;

    IF v_eunit_id IS NULL THEN
        RAISE EXCEPTION 'No available units for this equipment ID';
    END IF;

    -- Generate Transaction Code
    v_trans_code := 'TR-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || LPAD(FLOOR(RANDOM() * 1000)::TEXT, 3, '0');

    -- Insert into TRANSACTION table
    INSERT INTO transaction (student_id, user_id, transaction_code, transaction_date_borrowed)
    VALUES (p_student_id, p_user_id, v_trans_code, NOW())
    RETURNING transaction_id INTO v_transaction_id;

    -- Insert into BORROW_RECORD table
    INSERT INTO borrow_record (transaction_id, eunit_id, borrow_due_date, borrow_status)
    VALUES (v_transaction_id, v_eunit_id, p_due_date, 'borrowed');

    -- Update EQUIPMENT_UNIT status
    UPDATE equipment_unit SET eunit_status = 'borrowed', eunit_last_updated = NOW()
    WHERE eunit_id = v_eunit_id;

    -- Update EQUIPMENT total quantity (decrement stock)
    UPDATE equipment SET equipment_quantity = equipment_quantity - 1, equipment_last_updated = NOW()
    WHERE equipment_id = p_equipment_id;

EXCEPTION WHEN OTHERS THEN
    RAISE NOTICE 'Transaction failed: %', SQLERRM;
    RAISE;
END;
$;

-- 9. sp_process_borrow_by_unit (for C# app that selects specific unit)
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

-- 10. sp_process_return_boolean (for C# app using boolean condition)
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

-- 11. sp_update_overdue_statuses (from 06_procedures.sql)
CREATE OR REPLACE PROCEDURE sp_update_overdue_statuses()
LANGUAGE plpgsql AS $
BEGIN
    UPDATE borrow_record
    SET borrow_status = 'overdue', borrow_last_updated = NOW()
    WHERE borrow_status = 'borrowed' AND borrow_due_date < NOW();
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$;

-- 12. sp_mark_as_lost
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

-- 13. sp_update_unit_condition (UPDATED for 'For Review' workflow)
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
