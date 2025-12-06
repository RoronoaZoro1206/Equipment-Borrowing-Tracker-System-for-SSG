-- ============================================
-- SupremeShare - Equipment Borrowing Tracker System
-- COMPLETE DATABASE SCHEMA
-- PostgreSQL Database
-- Generated: 2025-12-02
-- ============================================
-- 
-- EXECUTION ORDER:
-- 1. Data Types and Enums
-- 2. Core Tables
-- 3. Additional Tables (Email Verification)
-- 4. Indexes
-- 5. Functions
-- 6. Triggers
-- 7. Stored Procedures
-- 8. Views
-- 9. Roles and Privileges (Optional - for production)
-- ============================================

-- ============================================
-- SECTION 1: DATA TYPES AND ENUMS
-- ============================================

-- User role enumeration
CREATE TYPE user_role_enum AS ENUM ('admin', 'support_admin');


-- ============================================
-- SECTION 2: CORE TABLES
-- ============================================

-- 1. TABLE: USER
CREATE TABLE "USER" (
    USER_ID SERIAL PRIMARY KEY,
    USER_USERNAME VARCHAR(50) UNIQUE NOT NULL,
    USER_HASHED_PASSWORD VARCHAR(255) NOT NULL,
    USER_ROLE user_role_enum NOT NULL,
    USER_POSITION VARCHAR(100) NOT NULL,
    USER_LAST_NAME VARCHAR(100) NOT NULL,
    USER_FIRST_NAME VARCHAR(100) NOT NULL,
    USER_MIDDLE_NAME VARCHAR(100),
    USER_SUFFIX VARCHAR(20),
    USER_EMAIL VARCHAR(100) UNIQUE,
    USER_CONTACT_NO VARCHAR(20),
    USER_STATUS BOOLEAN DEFAULT TRUE,
    USER_DATE_CREATED TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    USER_LAST_UPDATED TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 2. TABLE: STUDENT
CREATE TABLE STUDENT (
    STUDENT_ID SERIAL PRIMARY KEY,
    STUDENT_ID_NUMBER VARCHAR(20) UNIQUE NOT NULL,
    STUDENT_LAST_NAME VARCHAR(100) NOT NULL,
    STUDENT_FIRST_NAME VARCHAR(100) NOT NULL,
    STUDENT_MIDDLE_NAME VARCHAR(100),
    STUDENT_SUFFIX VARCHAR(20),
    STUDENT_COLLEGE VARCHAR(150) NOT NULL,
    STUDENT_COURSE VARCHAR(100),
    STUDENT_EMAIL VARCHAR(100),
    STUDENT_CONTACT_NO VARCHAR(20),
    STUDENT_STATUS BOOLEAN DEFAULT TRUE,
    STUDENT_DATE_ADDED TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    STUDENT_LAST_UPDATED TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 3. TABLE: EQUIPMENT
CREATE TABLE EQUIPMENT (
    EQUIPMENT_ID SERIAL PRIMARY KEY,
    EQUIPMENT_NAME VARCHAR(100) NOT NULL,
    EQUIPMENT_CATEGORY VARCHAR(50) NOT NULL,
    EQUIPMENT_QUANTITY INT NOT NULL,
    EQUIPMENT_DATE_ADDED TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    EQUIPMENT_LAST_UPDATED TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 4. TABLE: EQUIPMENT_UNIT
CREATE TABLE EQUIPMENT_UNIT (
    EUNIT_ID SERIAL PRIMARY KEY,
    EQUIPMENT_ID INT NOT NULL REFERENCES EQUIPMENT(EQUIPMENT_ID),
    EUNIT_SERIAL_NO VARCHAR(20) UNIQUE NOT NULL,
    EUNIT_CONDITION BOOLEAN NOT NULL,
    EUNIT_STATUS VARCHAR(50) NOT NULL,
    EUNIT_DATE_ADDED TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    EUNIT_LAST_UPDATED TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 5. TABLE: TRANSACTION
CREATE TABLE TRANSACTION (
    TRANSACTION_ID SERIAL PRIMARY KEY,
    STUDENT_ID INT NOT NULL REFERENCES STUDENT(STUDENT_ID),
    USER_ID INT NOT NULL REFERENCES "USER"(USER_ID),
    TRANSACTION_CODE VARCHAR(100) UNIQUE,
    TRANSACTION_DATE_BORROWED TIMESTAMP NOT NULL,
    TRANSACTION_REMARKS TEXT,
    TRANSACTION_DATE_ADDED TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    TRANSACTION_LAST_UPDATED TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 6. TABLE: BORROW_RECORD
CREATE TABLE BORROW_RECORD (
    BORROW_ID SERIAL PRIMARY KEY,
    TRANSACTION_ID INT NOT NULL REFERENCES TRANSACTION(TRANSACTION_ID),
    EUNIT_ID INT NOT NULL REFERENCES EQUIPMENT_UNIT(EUNIT_ID),
    BORROW_DUE_DATE TIMESTAMP NOT NULL,
    BORROW_DATE_RETURNED TIMESTAMP,
    BORROW_RETURN_CONDITION BOOLEAN,
    BORROW_STATUS VARCHAR(20) NOT NULL,
    BORROW_DATE_ADDED TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    BORROW_LAST_UPDATED TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 7. TABLE: AUDIT_LOG
CREATE TABLE AUDIT_LOG (
    AUDIT_ID SERIAL PRIMARY KEY,
    USER_ID INT REFERENCES "USER"(USER_ID) ON DELETE SET NULL,
    AUDIT_ACTION VARCHAR(255) NOT NULL,
    AUDIT_OLD_DATA TEXT,
    AUDIT_NEW_DATA TEXT,
    AUDIT_LOGGED_AT TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 8. TABLE: EMAIL_VERIFICATION_TOKEN
CREATE TABLE EMAIL_VERIFICATION_TOKEN (
    EVT_ID SERIAL PRIMARY KEY,
    USER_ID INTEGER NOT NULL REFERENCES "USER"(USER_ID) ON DELETE CASCADE,
    EVT_TOKEN VARCHAR(100) NOT NULL UNIQUE,
    EVT_NEW_EMAIL VARCHAR(100) NOT NULL,
    EVT_EXPIRES_AT TIMESTAMP WITH TIME ZONE NOT NULL,
    EVT_DATE_CREATED TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    EVT_IS_USED BOOLEAN DEFAULT FALSE
);


-- ============================================
-- SECTION 3: INDEXES
-- ============================================

-- Core table indexes
CREATE INDEX idx_equipment_unit_equipment_id ON EQUIPMENT_UNIT(EQUIPMENT_ID);
CREATE INDEX idx_transaction_student_id ON TRANSACTION(STUDENT_ID);
CREATE INDEX idx_transaction_user_id ON TRANSACTION(USER_ID);
CREATE INDEX idx_borrow_record_transaction_id ON BORROW_RECORD(TRANSACTION_ID);
CREATE INDEX idx_borrow_record_eunit_id ON BORROW_RECORD(EUNIT_ID);
CREATE INDEX idx_audit_log_user_id ON AUDIT_LOG(USER_ID);

-- Audit log additional indexes
CREATE INDEX IF NOT EXISTS idx_audit_log_action ON audit_log(audit_action);
CREATE INDEX IF NOT EXISTS idx_audit_log_logged_at ON audit_log(audit_logged_at DESC);

-- Email verification token indexes
CREATE INDEX IF NOT EXISTS idx_evt_user_id ON EMAIL_VERIFICATION_TOKEN(USER_ID);
CREATE INDEX IF NOT EXISTS idx_evt_token ON EMAIL_VERIFICATION_TOKEN(EVT_TOKEN);
CREATE INDEX IF NOT EXISTS idx_evt_expires ON EMAIL_VERIFICATION_TOKEN(EVT_EXPIRES_AT);

-- User email index (partial - only non-null emails)
CREATE INDEX IF NOT EXISTS idx_user_email ON "USER"(user_email) WHERE user_email IS NOT NULL;


-- ============================================
-- SECTION 4: FUNCTIONS
-- ============================================

-- 1. fn_get_available_stock
-- Returns count of available units for a specific equipment
CREATE OR REPLACE FUNCTION fn_get_available_stock(p_equipment_id INT)
RETURNS INT AS $$
DECLARE
    v_available_count INT;
BEGIN
    SELECT COUNT(*) INTO v_available_count
    FROM equipment_unit
    WHERE equipment_id = p_equipment_id 
      AND eunit_status = 'available';
    RETURN COALESCE(v_available_count, 0);
END;
$$ LANGUAGE plpgsql;

-- 2. fn_count_borrow_status
-- Returns count of borrow records with a specific status
CREATE OR REPLACE FUNCTION fn_count_borrow_status(p_status_type VARCHAR)
RETURNS INT AS $$
DECLARE
    v_count INT;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM borrow_record
    WHERE borrow_status = p_status_type;
    RETURN COALESCE(v_count, 0);
END;
$$ LANGUAGE plpgsql;

-- 3. fn_check_student_eligibility
-- Returns TRUE if student is active, FALSE if blocked or not found
CREATE OR REPLACE FUNCTION fn_check_student_eligibility(p_student_id INT)
RETURNS BOOLEAN AS $$
DECLARE
    v_status BOOLEAN;
BEGIN
    SELECT student_status INTO v_status
    FROM student
    WHERE student_id = p_student_id;
    RETURN COALESCE(v_status, FALSE);
END;
$$ LANGUAGE plpgsql;

-- 4. fn_calculate_days_overdue
-- Helper function to calculate days overdue for a borrow record
CREATE OR REPLACE FUNCTION fn_calculate_days_overdue(p_borrow_id INT)
RETURNS INT AS $$
DECLARE
    v_due_date TIMESTAMP;
    v_days INT;
BEGIN
    SELECT borrow_due_date INTO v_due_date 
    FROM borrow_record 
    WHERE borrow_id = p_borrow_id;
    
    IF v_due_date IS NULL THEN 
        RETURN 0; 
    END IF;
    
    v_days := EXTRACT(DAY FROM (NOW() - v_due_date))::INT;
    RETURN GREATEST(v_days, 0);
END;
$$ LANGUAGE plpgsql;


-- ============================================
-- SECTION 5: TRIGGER FUNCTIONS
-- ============================================

-- 5. fn_trigger_enforce_lost
-- When borrow status changes to 'lost': updates equipment unit status and blocks student
CREATE OR REPLACE FUNCTION fn_trigger_enforce_lost()
RETURNS TRIGGER AS $$
DECLARE
    v_student_id INT;
BEGIN
    IF NEW.borrow_status = 'lost' AND OLD.borrow_status != 'lost' THEN
        -- Update equipment unit status to 'lost'
        UPDATE equipment_unit
        SET eunit_status = 'lost'
        WHERE eunit_id = NEW.eunit_id;

        -- Find the student associated with this transaction
        SELECT student_id INTO v_student_id
        FROM transaction
        WHERE transaction_id = NEW.transaction_id;

        -- Block the student (set status to FALSE)
        UPDATE student
        SET student_status = FALSE
        WHERE student_id = v_student_id;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 6. fn_trigger_audit_borrow_status
-- Logs borrow status changes to audit_log
CREATE OR REPLACE FUNCTION fn_trigger_audit_borrow_status()
RETURNS TRIGGER AS $$
DECLARE
    v_current_user_id INT := 1; -- Default to User ID 1 (System/Admin)
BEGIN
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

-- 7. fn_log_timeline_activity
-- Logs detailed timeline activity for borrow status changes
CREATE OR REPLACE FUNCTION fn_log_timeline_activity()
RETURNS TRIGGER AS $$
DECLARE
    v_action_description VARCHAR(255);
    v_current_user_id INT;
BEGIN
    IF NEW.borrow_status IS DISTINCT FROM OLD.borrow_status THEN
        -- Construct readable description
        IF NEW.borrow_status = 'returned' THEN
            v_action_description := 'Item unit ' || NEW.eunit_id || ' returned.';
        ELSIF NEW.borrow_status = 'lost' THEN
            v_action_description := 'Item unit ' || NEW.eunit_id || ' marked as Lost/Missing.';
        ELSIF NEW.borrow_status = 'overdue' THEN
            v_action_description := 'Item unit ' || NEW.eunit_id || ' marked as Overdue.';
        ELSE
            v_action_description := 'Status changed to ' || NEW.borrow_status;
        END IF;

        -- Get current user from session or fallback to transaction owner
        BEGIN
            v_current_user_id := current_setting('app.current_user_id')::INT;
        EXCEPTION WHEN OTHERS THEN
            SELECT user_id INTO v_current_user_id 
            FROM transaction 
            WHERE transaction_id = NEW.transaction_id;
        END;

        -- Insert into AUDIT_LOG
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


-- ============================================
-- SECTION 6: TRIGGERS
-- ============================================

-- Trigger: Enforce Lost Protocol
DROP TRIGGER IF EXISTS trg_enforce_lost_protocol ON borrow_record;
CREATE TRIGGER trg_enforce_lost_protocol
AFTER UPDATE ON borrow_record
FOR EACH ROW
EXECUTE FUNCTION fn_trigger_enforce_lost();

-- Trigger: Audit Borrow Status Changes
DROP TRIGGER IF EXISTS trg_audit_borrow_status_change ON borrow_record;
CREATE TRIGGER trg_audit_borrow_status_change
AFTER UPDATE ON borrow_record
FOR EACH ROW
EXECUTE FUNCTION fn_trigger_audit_borrow_status();

-- Trigger: Log Timeline Activity
DROP TRIGGER IF EXISTS trg_log_timeline_activity ON borrow_record;
CREATE TRIGGER trg_log_timeline_activity
AFTER UPDATE ON borrow_record
FOR EACH ROW
EXECUTE FUNCTION fn_log_timeline_activity();



-- ============================================
-- SECTION 7: STORED PROCEDURES
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
    -- Find the first available unit for this equipment type
    SELECT eunit_id INTO v_eunit_id
    FROM equipment_unit
    WHERE equipment_id = p_equipment_id AND eunit_status = 'available'
    LIMIT 1;

    IF v_eunit_id IS NULL THEN
        RAISE EXCEPTION 'No available units for this equipment ID';
    END IF;

    -- Generate Transaction Code (e.g., TR-YYYYMMDD-Random)
    v_trans_code := 'TR-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || LPAD(FLOOR(RANDOM() * 1000)::TEXT, 3, '0');

    -- Insert into TRANSACTION table
    INSERT INTO transaction (student_id, user_id, transaction_code, transaction_date_borrowed)
    VALUES (p_student_id, p_user_id, v_trans_code, NOW())
    RETURNING transaction_id INTO v_transaction_id;

    -- Insert into BORROW_RECORD table
    INSERT INTO borrow_record (transaction_id, eunit_id, borrow_due_date, borrow_status)
    VALUES (v_transaction_id, v_eunit_id, p_due_date, 'borrowed');

    -- Update EQUIPMENT_UNIT status
    UPDATE equipment_unit
    SET eunit_status = 'borrowed', eunit_last_updated = NOW()
    WHERE eunit_id = v_eunit_id;

    -- Update EQUIPMENT total quantity (decrement stock)
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
    SELECT equipment_id INTO v_equipment_id 
    FROM equipment_unit
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
    UPDATE equipment_unit 
    SET eunit_status = 'borrowed', eunit_last_updated = NOW() 
    WHERE eunit_id = p_eunit_id;

    -- Decrement equipment quantity
    UPDATE equipment 
    SET equipment_quantity = equipment_quantity - 1, equipment_last_updated = NOW()
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
    -- Get the Unit ID and Equipment ID from the borrow record
    SELECT b.eunit_id, u.equipment_id
    INTO v_eunit_id, v_equipment_id
    FROM borrow_record b
    JOIN equipment_unit u ON b.eunit_id = u.eunit_id
    WHERE b.borrow_id = p_borrow_id;

    -- Determine new unit status based on condition
    IF p_return_condition = 'Good' THEN
        v_new_status := 'available';
    ELSE
        v_new_status := 'for review';
    END IF;

    -- Update BORROW_RECORD
    UPDATE borrow_record
    SET borrow_date_returned = NOW(),
        borrow_return_condition = (p_return_condition = 'Good'),
        borrow_status = 'returned',
        borrow_last_updated = NOW()
    WHERE borrow_id = p_borrow_id;

    -- Update EQUIPMENT_UNIT status
    UPDATE equipment_unit
    SET eunit_status = v_new_status,
        eunit_condition = (p_return_condition = 'Good'),
        eunit_last_updated = NOW()
    WHERE eunit_id = v_eunit_id;

    -- Update EQUIPMENT total quantity (only if available)
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
    SELECT b.eunit_id, u.equipment_id 
    INTO v_eunit_id, v_equipment_id
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
        UPDATE equipment 
        SET equipment_quantity = equipment_quantity + 1, equipment_last_updated = NOW()
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

--7.auto update status in admin status equipment unit
-- Run this in pgAdmin or your PostgreSQL client
CREATE OR REPLACE PROCEDURE sp_update_unit_condition(
    p_unit_id INT,
    p_condition BOOLEAN,
    p_user_id INT
)
LANGUAGE plpgsql AS $$
DECLARE
    v_old_condition BOOLEAN;
    v_old_status VARCHAR(50);
    v_new_status VARCHAR(50);
BEGIN
    SELECT eunit_condition, eunit_status 
    INTO v_old_condition, v_old_status
    FROM equipment_unit 
    WHERE eunit_id = p_unit_id;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Unit not found';
    END IF;
    
    IF p_condition = TRUE AND v_old_status = 'maintenance' THEN
        v_new_status := 'available';
    ELSIF p_condition = FALSE AND v_old_status = 'available' THEN
        v_new_status := 'maintenance';
    ELSE
        v_new_status := v_old_status;
    END IF;
    
    UPDATE equipment_unit 
    SET eunit_condition = p_condition,
        eunit_status = v_new_status,
        eunit_last_updated = NOW()
    WHERE eunit_id = p_unit_id;
    
    INSERT INTO audit_log (user_id, audit_action, audit_old_data, audit_new_data, audit_logged_at)
    VALUES (
        p_user_id,
        'Unit condition updated for ID ' || p_unit_id,
        'Condition: ' || CASE WHEN v_old_condition THEN 'Good' ELSE 'Damaged' END || ', Status: ' || v_old_status,
        'Condition: ' || CASE WHEN p_condition THEN 'Good' ELSE 'Damaged' END || ', Status: ' || v_new_status,
        NOW()
    );
END;
$$;


-- ============================================
-- SECTION 8: VIEWS
-- ============================================

-- 1. transaction_borrowing_info_view
-- Shows borrowing information with equipment details
CREATE OR REPLACE VIEW transaction_borrowing_info_view AS
SELECT
    e.equipment_category,
    e.equipment_name,
    u.eunit_serial_no,
    u.eunit_status,
    b.borrow_due_date,
    b.borrow_date_returned,
    b.borrow_return_condition,
    b.borrow_last_updated
FROM borrow_record b
JOIN equipment_unit u ON b.eunit_id = u.eunit_id
JOIN equipment e ON u.equipment_id = e.equipment_id
JOIN transaction t ON b.transaction_id = t.transaction_id;

-- 2. admin_user_management_view
-- Shows user information for admin management (excludes password)
CREATE OR REPLACE VIEW admin_user_management_view AS
SELECT
    user_id,
    user_username,
    user_last_name,
    user_first_name,
    user_middle_name,
    user_suffix,
    user_role,
    user_position,
    user_email,
    user_contact_no,
    user_status
FROM "USER";


-- ============================================
-- SECTION 9: TABLE COMMENTS
-- ============================================

-- Audit Log Comments
COMMENT ON TABLE audit_log IS 'Stores audit trail of all system actions for compliance and tracking';
COMMENT ON COLUMN audit_log.audit_id IS 'Primary key';
COMMENT ON COLUMN audit_log.user_id IS 'User who performed the action (NULL for system actions)';
COMMENT ON COLUMN audit_log.audit_action IS 'Description of the action performed';
COMMENT ON COLUMN audit_log.audit_old_data IS 'JSON representation of data before change';
COMMENT ON COLUMN audit_log.audit_new_data IS 'JSON representation of data after change';
COMMENT ON COLUMN audit_log.audit_logged_at IS 'Timestamp when the action was logged';

-- Email Verification Token Comments
COMMENT ON TABLE EMAIL_VERIFICATION_TOKEN IS 'Stores pending email verification tokens for email change requests';
COMMENT ON COLUMN EMAIL_VERIFICATION_TOKEN.EVT_TOKEN IS 'Unique verification token sent via email';
COMMENT ON COLUMN EMAIL_VERIFICATION_TOKEN.EVT_NEW_EMAIL IS 'The new email address to be verified';
COMMENT ON COLUMN EMAIL_VERIFICATION_TOKEN.EVT_EXPIRES_AT IS 'Token expiration timestamp (24 hours from creation)';
COMMENT ON COLUMN EMAIL_VERIFICATION_TOKEN.EVT_IS_USED IS 'Whether the token has been used for verification';


-- ============================================
-- SECTION 10: ROLES AND PRIVILEGES (OPTIONAL)
-- Uncomment if needed for production deployment
-- ============================================

/*
-- Create roles
CREATE ROLE admin_role;
CREATE ROLE support_admin_role;

-- Create users and assign roles
CREATE ROLE admin_user LOGIN PASSWORD 'secure_admin_password';
GRANT admin_role TO admin_user;

CREATE ROLE support_user LOGIN PASSWORD 'secure_support_password';
GRANT support_admin_role TO support_user;

-- Grant privileges to admin_role
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO admin_role;

-- Grant privileges to support_admin_role
GRANT SELECT, INSERT, UPDATE ON student TO support_admin_role;
GRANT SELECT, INSERT, UPDATE ON transaction TO support_admin_role;
GRANT SELECT, INSERT, UPDATE ON borrow_record TO support_admin_role;

-- Audit schema access
GRANT USAGE ON SCHEMA audit TO admin_role;
REVOKE ALL ON SCHEMA audit FROM support_admin_role;
*/


-- ============================================
-- END OF COMPLETE DATABASE SCHEMA
-- ============================================


-- ============================================
-- SECTION 11: QUICK SELECT QUERIES
-- Uncomment and run individually as needed
-- ============================================

-- SELECT * FROM "USER";
-- SELECT * FROM STUDENT;
-- SELECT * FROM EQUIPMENT;
-- SELECT * FROM EQUIPMENT_UNIT;
-- SELECT * FROM TRANSACTION;
-- SELECT * FROM BORROW_RECORD;
-- SELECT * FROM AUDIT_LOG;
-- SELECT * FROM EMAIL_VERIFICATION_TOKEN;

-- Views
-- SELECT * FROM transaction_borrowing_info_view;
-- SELECT * FROM admin_user_management_view;
