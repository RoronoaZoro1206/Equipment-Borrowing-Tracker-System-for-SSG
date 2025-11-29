1. Enforce Lost Protocol (trg_enforce_lost_protocol): Automatically marks equipment as lost and blocks the student when a borrow record is marked "lost".

CREATE OR REPLACE FUNCTION fn_trigger_enforce_lost() RETURNS TRIGGER AS $$
DECLARE v_student_id INT;
BEGIN
    IF NEW.borrow_status = 'lost' AND OLD.borrow_status != 'lost' THEN
        UPDATE equipment_unit SET eunit_status = 'lost' WHERE eunit_id = NEW.eunit_id;
        SELECT student_id INTO v_student_id FROM transaction WHERE transaction_id = NEW.transaction_id;
        UPDATE student SET student_status = FALSE WHERE student_id = v_student_id; -- Set to blocked/inactive
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_enforce_lost_protocol
AFTER UPDATE ON borrow_record
FOR EACH ROW EXECUTE FUNCTION fn_trigger_enforce_lost();

2. Audit Borrow Status (trg_audit_borrow_status_change): Logs status changes to AUDIT_LOG.

CREATE OR REPLACE FUNCTION fn_trigger_audit_borrow_status() RETURNS TRIGGER AS $$
DECLARE v_current_user_id INT := 1; -- Default system admin
BEGIN
    IF NEW.borrow_status IS DISTINCT FROM OLD.borrow_status THEN
        INSERT INTO AUDIT_LOG (USER_ID, AUDIT_ACTION, AUDIT_OLD_DATA, AUDIT_NEW_DATA, AUDIT_LOGGED_AT)
        VALUES (v_current_user_id, 'Borrow Status Changed for Record ID ' || NEW.borrow_id, OLD.borrow_status, NEW.borrow_status, NOW());
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

