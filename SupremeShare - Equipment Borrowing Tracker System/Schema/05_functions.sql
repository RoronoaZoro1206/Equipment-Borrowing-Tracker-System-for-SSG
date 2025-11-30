-- ============================================
-- Stored Functions
-- ============================================

-- 1. fn_get_available_stock
CREATE OR REPLACE FUNCTION fn_get_available_stock(p_equipment_id INT)
RETURNS INT AS $$
DECLARE
    v_available_count INT;
BEGIN
    -- Count specific units that are currently marked as 'available'
    SELECT COUNT(*) INTO v_available_count
    FROM equipment_unit
    WHERE equipment_id = p_equipment_id 
      AND eunit_status = 'available';

    -- Return the count (0 if none found)
    RETURN COALESCE(v_available_count, 0);
END;
$$ LANGUAGE plpgsql;

-- 2. fn_count_borrow_status
CREATE OR REPLACE FUNCTION fn_count_borrow_status(p_status_type VARCHAR)
RETURNS INT AS $$
DECLARE
    v_count INT;
BEGIN
    -- Count records that match the requested status
    SELECT COUNT(*) INTO v_count
    FROM borrow_record
    WHERE borrow_status = p_status_type;

    -- Return the count (0 if no records found)
    RETURN COALESCE(v_count, 0);
END;
$$ LANGUAGE plpgsql;

-- 3. fn_check_student_eligibility
CREATE OR REPLACE FUNCTION fn_check_student_eligibility(p_student_id INT)
RETURNS BOOLEAN AS $$
DECLARE
    v_status BOOLEAN;
BEGIN
    -- Retrieve the status of the student (BOOLEAN: TRUE = Active, FALSE = Blocked)
    SELECT student_status INTO v_status
    FROM student
    WHERE student_id = p_student_id;

    -- Return the status directly (TRUE if active, FALSE if blocked or not found)
    RETURN COALESCE(v_status, FALSE);
END;
$$ LANGUAGE plpgsql;

-- 4. fn_calculate_days_overdue (helper function)
CREATE OR REPLACE FUNCTION fn_calculate_days_overdue(p_borrow_id INT)
RETURNS INT AS $$
DECLARE
    v_due_date TIMESTAMP;
    v_days INT;
BEGIN
    SELECT borrow_due_date INTO v_due_date FROM borrow_record WHERE borrow_id = p_borrow_id;
    IF v_due_date IS NULL THEN RETURN 0; END IF;
    v_days := EXTRACT(DAY FROM (NOW() - v_due_date))::INT;
    RETURN GREATEST(v_days, 0);
END;
$$ LANGUAGE plpgsql;

-- =============================================
-- TRIGGERS
-- =============================================

-- 5. fn_trigger_enforce_lost
CREATE OR REPLACE FUNCTION fn_trigger_enforce_lost()
RETURNS TRIGGER AS $$
DECLARE
    v_student_id INT;
BEGIN
    -- Check if the status was changed to 'lost'
    IF NEW.borrow_status = 'lost' AND OLD.borrow_status != 'lost' THEN
        -- 1. Update the Equipment Unit status to 'lost'
        UPDATE equipment_unit
        SET eunit_status = 'lost'
        WHERE eunit_id = NEW.eunit_id;

        -- 2. Find the student associated with this transaction
        SELECT student_id INTO v_student_id
        FROM transaction
        WHERE transaction_id = NEW.transaction_id;

        -- 3. Block the student (set status to FALSE)
        UPDATE student
        SET student_status = FALSE
        WHERE student_id = v_student_id;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Bind the Function to the Table via a Trigger
DROP TRIGGER IF EXISTS trg_enforce_lost_protocol ON borrow_record;
CREATE TRIGGER trg_enforce_lost_protocol
AFTER UPDATE ON borrow_record
FOR EACH ROW
EXECUTE FUNCTION fn_trigger_enforce_lost();

-- 6. fn_trigger_audit_borrow_status
CREATE OR REPLACE FUNCTION fn_trigger_audit_borrow_status()
RETURNS TRIGGER AS $$
DECLARE
    v_current_user_id INT := 1; -- Default to User ID 1 (System/Admin)
BEGIN
    -- Check if the borrow_status column has changed
    IF NEW.borrow_status IS DISTINCT FROM OLD.borrow_status THEN
        INSERT INTO audit_log (user_id, audit_action, audit_old_data, audit_new_data, audit_logged_at)
        VALUES (
            v_current_user_id,
            'Borrow Status Changed for Record ID ' || NEW.borrow_id,
            OLD.borrow_status,
            NEW.borrow_status,
            NOW()
        );
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Bind the Function to the Table via a Trigger
DROP TRIGGER IF EXISTS trg_audit_borrow_status_change ON borrow_record;
CREATE TRIGGER trg_audit_borrow_status_change
AFTER UPDATE ON borrow_record
FOR EACH ROW
EXECUTE FUNCTION fn_trigger_audit_borrow_status();

-- 7. fn_log_timeline_activity
CREATE OR REPLACE FUNCTION fn_log_timeline_activity()
RETURNS TRIGGER AS $$
DECLARE
    v_action_description VARCHAR(255);
    v_current_user_id INT;
BEGIN
    -- Only proceed if the status has actually changed
    IF NEW.borrow_status IS DISTINCT FROM OLD.borrow_status THEN
        -- 1. Construct a readable description for the Timeline
        IF NEW.borrow_status = 'returned' THEN
            v_action_description := 'Item unit ' || NEW.eunit_id || ' returned.';
        ELSIF NEW.borrow_status = 'lost' THEN
            v_action_description := 'Item unit ' || NEW.eunit_id || ' marked as Lost/Missing.';
        ELSIF NEW.borrow_status = 'overdue' THEN
            v_action_description := 'Item unit ' || NEW.eunit_id || ' marked as Overdue.';
        ELSE
            v_action_description := 'Status changed to ' || NEW.borrow_status;
        END IF;

        -- 2. Identify the User
        BEGIN
            v_current_user_id := current_setting('app.current_user_id')::INT;
        EXCEPTION WHEN OTHERS THEN
            SELECT user_id INTO v_current_user_id 
            FROM transaction WHERE transaction_id = NEW.transaction_id;
        END;

        -- 3. Insert into AUDIT_LOG
        INSERT INTO audit_log (user_id, audit_action, audit_old_data, audit_new_data, audit_logged_at)
        VALUES (
            v_current_user_id,
            v_action_description,
            'Status: ' || OLD.borrow_status,
            'Status: ' || NEW.borrow_status,
            NOW()
        );
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Bind the Function to the Table via a Trigger
DROP TRIGGER IF EXISTS trg_log_timeline_activity ON borrow_record;
CREATE TRIGGER trg_log_timeline_activity
AFTER UPDATE ON borrow_record
FOR EACH ROW
EXECUTE FUNCTION fn_log_timeline_activity();
