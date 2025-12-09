1. sp_process_borrow_transaction: Atomic operation to create transaction, borrow record, update unit status, and decrement inventory.

CREATE OR REPLACE PROCEDURE sp_process_borrow_transaction(
    p_student_id INT, p_user_id INT, p_equipment_id INT, p_due_date TIMESTAMP
) LANGUAGE plpgsql AS $$
DECLARE
    v_transaction_id INT;
    v_eunit_id INT;
    v_trans_code VARCHAR(100);
BEGIN
    -- Find available unit
    SELECT eunit_id INTO v_eunit_id FROM equipment_unit
    WHERE equipment_id = p_equipment_id AND eunit_status = 'available' LIMIT 1;
    
    IF v_eunit_id IS NULL THEN RAISE EXCEPTION 'No available units'; END IF;

    v_trans_code := 'TR-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || FLOOR(RANDOM() * 1000)::TEXT;

    INSERT INTO transaction (student_id, user_id, transaction_code, transaction_date_borrowed)
    VALUES (p_student_id, p_user_id, v_trans_code, NOW())
    RETURNING transaction_id INTO v_transaction_id;

    INSERT INTO borrow_record (transaction_id, eunit_id, borrow_due_date, borrow_status)
    VALUES (v_transaction_id, v_eunit_id, p_due_date, 'borrowed');

    UPDATE equipment_unit SET eunit_status = 'borrowed' WHERE eunit_id = v_eunit_id;
    UPDATE equipment SET equipment_quantity = equipment_quantity - 1 WHERE equipment_id = p_equipment_id;
EXCEPTION WHEN OTHERS THEN RAISE NOTICE 'Transaction failed: %', SQLERRM; ROLLBACK;
END;
$$;


I have converted the PDF content into a structured Markdown (.md) file.

Markdown is superior to a plain text file for this purpose because it preserves the SQL code formatting, tables, and hierarchy, which makes it much easier for an AI (and humans) to read and interpret later.

You can copy the code block below and save it as SupremeShare_Database_Design.md.

File Content: SupremeShare_Database_Design.md
Markdown

# SupremeShare: Equipment Borrowing Tracker System
**Group 3 - Database Design Plan**
**Institution:** Cebu Technological University - Main Campus
**College:** College of Computer, Information and Communications Technology
**Course:** PC317: Database Management Systems

**Group Members:**
* [cite_start]Geonzon, Princess Kaye S. (Database Architect) [cite: 14, 15]
* [cite_start]Baguio, Kent RG (Data Modeler) [cite: 16]
* [cite_start]Sagadsad, Rosheen (Database Security Specialist) [cite: 17]
* [cite_start]Ramirez, Lawrence (Stored Routines Developer) [cite: 18]
* [cite_start]Viscaya, Wency (Quality Assurance and Tester) [cite: 18]

---

## I. Introduction and System Overview

### System Overview
[cite_start]SupremeShare is an Equipment Borrowing Tracker System designed for the Supreme Student Government (SSG) Office of Cebu Technological University Main Campus[cite: 32]. [cite_start]It digitizes the borrowing and returning transactions of university-owned items, such as microphones, speakers, and projectors[cite: 33].

[cite_start]The system enables authorized users (SSG Officers/Administrators and support staff) to manage borrowing records efficiently by tracing who borrowed the equipment, when, and what was borrowed[cite: 34]. [cite_start]It allows for status updates on equipment condition, tracking of damaged materials, and monitoring operations via activity logs[cite: 35, 36].

### Scope
The system stores and manages data on:
* [cite_start]System users and user activities [cite: 39]
* [cite_start]Student borrowers [cite: 39]
* [cite_start]Equipment inventory [cite: 39]
* [cite_start]Borrowing and return transactions [cite: 39]

[cite_start]**Exclusions:** The system does *not* cover financial penalties, automated SMS/Email notifications, barcode scanning, online reservation, maintenance scheduling, or integration with external university systems[cite: 41].

### System Justification
[cite_start]The current manual process causes problems with tracking items and increases the chance of losing school property[cite: 44]. [cite_start]Manual tracking leads to delays and double-booking[cite: 46]. [cite_start]SupremeShare moves this process online to a central database to track transactions in real-time, reducing lost or overdue items[cite: 47, 48].

### [cite_start]Key Actors / Users [cite: 52, 61, 62]

| Actor/User | Role Description | Access Level |
| :--- | :--- | :--- |
| **Admin** | Main system overseer (SSG Officer). Manages all data including users, inventory, and borrowers. | **Full Access:** Add, edit, delete, approve, manage all records. |
| **Support Admin** | Assistant students/personnel responsible for daily operations. Records borrow/return transactions and updates statuses. | **Limited Modify Access:** Can add/update borrow/return logs. Cannot delete major records or modify system settings. |
| **Student/Borrower** | A university student who borrows equipment. | **View-Only (Indirect):** No login. Records are managed by Admins. |

---

## II. Conceptual Design & Data Dictionary

[cite_start]The database follows a normalized structure linking Users, Students, Equipment, Transactions, and Audit Logs[cite: 220].

### Data Dictionary

#### [cite_start]Table: USER [cite: 224, 234]
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `USER_ID` | SERIAL | PK, Auto_Increment | Unique identifier for system user. |
| `USER_USERNAME` | VARCHAR(50) | UNIQUE, NOT NULL | Unique login name. |
| `USER_HASHED_PASSWORD` | VARCHAR(255) | NOT NULL | Encrypted password (bcrypt). |
| `USER_ROLE` | ENUM | NOT NULL | 'admin' or 'support_admin'. |
| `USER_POSITION` | VARCHAR(100) | NOT NULL | Role/Title in organization. |
| `USER_LAST_NAME` | VARCHAR(100) | NOT NULL | User's last name. |
| `USER_FIRST_NAME` | VARCHAR(100) | NOT NULL | User's first name. |
| `USER_MIDDLE_NAME` | VARCHAR(100) | NULL | Optional middle name. |
| `USER_SUFFIX` | VARCHAR(20) | NULL | Optional suffix. |
| `USER_EMAIL` | VARCHAR(100) | NULL | User email address. |
| `USER_CONTACT_NO` | VARCHAR(20) | NULL | User contact number. |
| `USER_STATUS` | BOOLEAN | Default 1 | True = Active, False = Inactive. |
| `USER_DATE_CREATED` | TIMESTAMP | Default Current_Timestamp | Account creation date. |
| `USER_LAST_UPDATED` | TIMESTAMP | On Update Current_Timestamp | Latest update date. |

#### [cite_start]Table: STUDENT [cite: 245, 255]
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `STUDENT_ID` | SERIAL | PK, Auto_Increment | Internal system ID. |
| `STUDENT_ID_NUMBER` | VARCHAR(20) | UNIQUE, NOT NULL | Official University ID number. |
| `STUDENT_LAST_NAME` | VARCHAR(100) | NOT NULL | Student's last name. |
| `STUDENT_FIRST_NAME` | VARCHAR(100) | NOT NULL | Student's first name. |
| `STUDENT_COLLEGE` | VARCHAR(150) | NOT NULL | College the student belongs to. |
| `STUDENT_STATUS` | BOOLEAN | Default 1 | True = Active, False = Inactive/Blocked. |
| *Other fields:* | *Includes Middle Name, Suffix, Course, Email, Contact No, Timestamps.* | | |

#### [cite_start]Table: EQUIPMENT [cite: 257]
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `EQUIPMENT_ID` | SERIAL | PK, Auto_Increment | Unique ID for equipment type. |
| `EQUIPMENT_NAME` | VARCHAR(100) | NOT NULL | Name (e.g., Laptop). |
| `EQUIPMENT_CATEGORY` | VARCHAR(50) | NOT NULL | Type/Category. |
| `EQUIPMENT_QUANTITY` | INT | NOT NULL | Total units available. |

#### [cite_start]Table: EQUIPMENT_UNIT [cite: 259, 268]
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `EUNIT_ID` | SERIAL | PK, Auto_Increment | Unique ID for specific unit. |
| `EQUIPMENT_ID` | INT | FK -> Equipment | Links to equipment type. |
| `EUNIT_SERIAL_NO` | VARCHAR(20) | NOT NULL, UNIQUE | Physical serial number. |
| `EUNIT_CONDITION` | BOOLEAN | NOT NULL | True = Good, False = Damaged. |
| `EUNIT_STATUS` | VARCHAR(50) | NOT NULL | 'available', 'borrowed', etc. |

#### [cite_start]Table: TRANSACTION [cite: 270]
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `TRANSACTION_ID` | SERIAL | PK, Auto_Increment | Unique ID. |
| `STUDENT_ID` | INT | FK -> Student | Borrower. |
| `USER_ID` | INT | FK -> User | Staff handling transaction. |
| `TRANSACTION_CODE` | VARCHAR(100) | UNIQUE | System generated code. |
| `TRANSACTION_DATE_BORROWED` | TIMESTAMP | NOT NULL | Date borrowed. |

#### [cite_start]Table: BORROW_RECORD [cite: 281]
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `BORROW_ID` | SERIAL | PK, Auto_Increment | Unique ID. |
| `TRANSACTION_ID` | INT | FK -> Transaction | Parent transaction. |
| `EUNIT_ID` | INT | FK -> Equipment_Unit | Specific item borrowed. |
| `BORROW_DUE_DATE` | TIMESTAMP | NOT NULL | Expected return. |
| `BORROW_STATUS` | VARCHAR(20) | NOT NULL | 'borrowed', 'returned', 'overdue'. |

#### [cite_start]Table: AUDIT_LOG [cite: 283]
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `AUDIT_ID` | SERIAL | PK | Unique ID. |
| `USER_ID` | INT | FK -> User | User performing action. |
| `AUDIT_ACTION` | VARCHAR(255) | NOT NULL | Description of action. |
| `AUDIT_OLD_DATA` | TEXT | NULL | Data before change (Encrypted). |
| `AUDIT_NEW_DATA` | TEXT | NULL | Data after change. |

---

## III. Physical Implementation & SQL Schema

**Technical Decisions:**
* [cite_start]**RBAC:** Strict separation between `admin_role` and `support_admin_role`[cite: 320].
* **Security:** Passwords hashed with bcrypt; SSL/TLS for connections; [cite_start]PII encrypted[cite: 587, 593, 596].
* **Indexing:** Implicit indexing on PKs and Unique columns; [cite_start]Explicit indexing on all Foreign Keys[cite: 316, 317].

### [cite_start]SQL Schema Construction [cite: 343 - 528]

```sql
-- 1. Create Enums
CREATE TYPE user_role_enum AS ENUM ('admin', 'support_admin');
CREATE TYPE borrow_status_enum AS ENUM ('borrowed', 'returned', 'overdue', 'lost');

-- 2. Create Tables

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
    USER_EMAIL VARCHAR(100),
    USER_CONTACT_NO VARCHAR(20),
    USER_STATUS BOOLEAN DEFAULT TRUE, -- TRUE = ACTIVE
    USER_DATE_CREATED TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    USER_LAST_UPDATED TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

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
    STUDENT_STATUS BOOLEAN DEFAULT TRUE, -- TRUE = ACTIVE
    STUDENT_DATE_ADDED TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    STUDENT_LAST_UPDATED TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE EQUIPMENT (
    EQUIPMENT_ID SERIAL PRIMARY KEY,
    EQUIPMENT_NAME VARCHAR(100) NOT NULL,
    EQUIPMENT_CATEGORY VARCHAR(50) NOT NULL,
    EQUIPMENT_QUANTITY INT NOT NULL,
    EQUIPMENT_DATE_ADDED TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    EQUIPMENT_LAST_UPDATED TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE EQUIPMENT_UNIT (
    EUNIT_ID SERIAL PRIMARY KEY,
    EQUIPMENT_ID INT NOT NULL REFERENCES EQUIPMENT(EQUIPMENT_ID),
    EUNIT_SERIAL_NO VARCHAR(20) UNIQUE NOT NULL,
    EUNIT_CONDITION BOOLEAN NOT NULL, -- TRUE = GOOD
    EUNIT_STATUS VARCHAR(50) NOT NULL, -- 'available', 'borrowed', etc.
    EUNIT_DATE_ADDED TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    EUNIT_LAST_UPDATED TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

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

CREATE TABLE BORROW_RECORD (
    BORROW_ID SERIAL PRIMARY KEY,
    TRANSACTION_ID INT NOT NULL REFERENCES TRANSACTION(TRANSACTION_ID),
    EUNIT_ID INT NOT NULL REFERENCES EQUIPMENT_UNIT(EUNIT_ID),
    BORROW_DUE_DATE TIMESTAMP NOT NULL,
    BORROW_DATE_RETURNED TIMESTAMP,
    BORROW_RETURN_CONDITION BOOLEAN,
    BORROW_STATUS VARCHAR(20) NOT NULL, -- Cast to ENUM recommended in logic
    BORROW_DATE_ADDED TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    BORROW_LAST_UPDATED TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE AUDIT_LOG (
    AUDIT_ID SERIAL PRIMARY KEY,
    USER_ID INT NOT NULL REFERENCES "USER"(USER_ID),
    AUDIT_ACTION VARCHAR(255) NOT NULL,
    AUDIT_OLD_DATA TEXT,
    AUDIT_NEW_DATA TEXT,
    AUDIT_LOGGED_AT TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 3. Indexes
CREATE INDEX idx_equipment_unit_equipment_id ON EQUIPMENT_UNIT(EQUIPMENT_ID);
CREATE INDEX idx_transaction_student_id ON TRANSACTION(STUDENT_ID);
CREATE INDEX idx_transaction_user_id ON TRANSACTION(USER_ID);
CREATE INDEX idx_borrow_record_transaction_id ON BORROW_RECORD(TRANSACTION_ID);
CREATE INDEX idx_borrow_record_eunit_id ON BORROW_RECORD(EUNIT_ID);
CREATE INDEX idx_audit_log_user_id ON AUDIT_LOG(USER_ID);
IV. Stored Routines
Stored Functions 

1. fn_get_available_stock: Calculates real-time available quantity of a specific equipment type.

SQL

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
2. fn_count_borrow_status: Returns total count of transactions filtered by status (for Dashboard).

SQL

CREATE OR REPLACE FUNCTION fn_count_borrow_status(p_status_type VARCHAR)
RETURNS INT AS $$
DECLARE
    v_count INT;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM borrow_record
    WHERE borrow_status = p_status_type::borrow_status_enum;
    RETURN COALESCE(v_count, 0);
END;
$$ LANGUAGE plpgsql;
3. fn_check_student_eligibility: Returns TRUE if student is active, FALSE if blocked.

SQL

CREATE OR REPLACE FUNCTION fn_check_student_eligibility(p_student_id INT)
RETURNS BOOLEAN AS $$
DECLARE
    v_status VARCHAR(20); -- Note: Schema defines this as BOOLEAN, logic may need adjustment to check boolean
BEGIN
    SELECT student_status INTO v_status FROM student WHERE student_id = p_student_id;
    -- Assuming logic maps status to active/blocked
    IF v_status = 'active' THEN RETURN TRUE; ELSE RETURN FALSE; END IF;
END;
$$ LANGUAGE plpgsql;
Triggers 

1. Enforce Lost Protocol (trg_enforce_lost_protocol): Automatically marks equipment as lost and blocks the student when a borrow record is marked "lost".

SQL

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

SQL

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
Stored Procedures 

1. sp_process_borrow_transaction: Atomic operation to create transaction, borrow record, update unit status, and decrement inventory.

SQL

CREATE OR REPLACE PROCEDURE sp_process_borrow_transaction(
    p_student_id INT, p_user_id INT, p_equipment_id INT, p_due_date TIMESTAMP
) LANGUAGE plpgsql AS $$
DECLARE
    v_transaction_id INT;
    v_eunit_id INT;
    v_trans_code VARCHAR(100);
BEGIN
    -- Find available unit
    SELECT eunit_id INTO v_eunit_id FROM equipment_unit
    WHERE equipment_id = p_equipment_id AND eunit_status = 'available' LIMIT 1;
    
    IF v_eunit_id IS NULL THEN RAISE EXCEPTION 'No available units'; END IF;

    v_trans_code := 'TR-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || FLOOR(RANDOM() * 1000)::TEXT;

    INSERT INTO transaction (student_id, user_id, transaction_code, transaction_date_borrowed)
    VALUES (p_student_id, p_user_id, v_trans_code, NOW())
    RETURNING transaction_id INTO v_transaction_id;

    INSERT INTO borrow_record (transaction_id, eunit_id, borrow_due_date, borrow_status)
    VALUES (v_transaction_id, v_eunit_id, p_due_date, 'borrowed');

    UPDATE equipment_unit SET eunit_status = 'borrowed' WHERE eunit_id = v_eunit_id;
    UPDATE equipment SET equipment_quantity = equipment_quantity - 1 WHERE equipment_id = p_equipment_id;
EXCEPTION WHEN OTHERS THEN RAISE NOTICE 'Transaction failed: %', SQLERRM; ROLLBACK;
END;
$$;

2. sp_process_return_boolean: Updates return date, condition, and inventory count atomically using boolean condition.
   Damaged items (FALSE) go to 'for review' status per group agreement.

CREATE OR REPLACE PROCEDURE sp_process_return_boolean(
    p_borrow_id INT, p_return_condition BOOLEAN, p_user_id INT
) LANGUAGE plpgsql AS $$
DECLARE
    v_eunit_id INT; v_equipment_id INT; v_new_status VARCHAR(50);
BEGIN
    SELECT b.eunit_id, u.equipment_id INTO v_eunit_id, v_equipment_id
    FROM borrow_record b JOIN equipment_unit u ON b.eunit_id = u.eunit_id
    WHERE b.borrow_id = p_borrow_id AND b.borrow_status IN ('borrowed', 'overdue');

    IF p_return_condition = 'Good' THEN v_new_status := 'available';
    ELSE v_new_status := 'for review'; END IF;

    UPDATE borrow_record SET borrow_date_returned = NOW(), borrow_return_condition = (p_return_condition = 'Good'), borrow_status = 'returned'
    WHERE borrow_id = p_borrow_id;

    UPDATE equipment_unit SET eunit_status = v_new_status, eunit_condition = (p_return_condition = 'Good')
    WHERE eunit_id = v_eunit_id;

    IF v_new_status = 'available' THEN
        UPDATE equipment SET equipment_quantity = equipment_quantity + 1 WHERE equipment_id = v_equipment_id;
    END IF;
EXCEPTION WHEN OTHERS THEN RAISE NOTICE 'Return failed'; ROLLBACK;
END;
$$;