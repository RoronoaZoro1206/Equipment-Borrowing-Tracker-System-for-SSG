# SupremeShare: Equipment Borrowing Tracker System
**Group 3 - Database Design Plan**
**Institution:** Cebu Technological University - Main Campus
**College:** College of Computer, Information and Communications Technology
**Course:** PC317: Database Management Systems

**Group Members:**
* Geonzon, Princess Kaye S. (Database Architect)
* Baguio, Kent RG (Data Modeler)
* Sagadsad, Rosheen (Database Security Specialist)
* Ramirez, Lawrence (Stored Routines Developer)
* Viscaya, Wency (Quality Assurance and Tester)

---

## I. Introduction and System Overview

### System Overview
SupremeShare is an Equipment Borrowing Tracker System designed for the Supreme Student Government (SSG) Office of Cebu Technological University Main Campus. It digitizes the borrowing and returning transactions of university-owned items, such as microphones, speakers, and projectors.

The system enables authorized users (SSG Officers/Administrators and support staff) to manage borrowing records efficiently by tracing who borrowed the equipment, when, and what was borrowed. It allows for status updates on equipment condition, tracking of damaged materials, and monitoring operations via activity logs.

### Scope
The system stores and manages data on:
* System users and user activities
* Student borrowers
* Equipment inventory
* Borrowing and return transactions

**Exclusions:** The system does *not* cover financial penalties, automated SMS/Email notifications, barcode scanning, online reservation, maintenance scheduling, or integration with external university systems.

### System Justification
The current manual process causes problems with tracking items and increases the chance of losing school property. Manual tracking leads to delays and double-booking. SupremeShare moves this process online to a central database to track transactions in real-time, reducing lost or overdue items.

### Key Actors / Users

| Actor/User | Role Description | Access Level |
| :--- | :--- | :--- |
| **Admin** | Main system overseer (SSG Officer). Manages all data including users, inventory, and borrowers. | **Full Access:** Add, edit, delete, approve, manage all records. |
| **Support Admin** | Assistant students/personnel responsible for daily operations. Records borrow/return transactions and updates statuses. | **Limited Modify Access:** Can add/update borrow/return logs. Cannot delete major records or modify system settings. |
| **Student/Borrower** | A university student who borrows equipment. | **View-Only (Indirect):** No login. Records are managed by Admins. |

---

## II. Conceptual Design & Data Dictionary

The database follows a normalized structure linking Users, Students, Equipment, Transactions, and Audit Logs.

### Data Dictionary

#### Table: USER
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

#### Table: STUDENT
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `STUDENT_ID` | SERIAL | PK, Auto_Increment | Internal system ID. |
| `STUDENT_ID_NUMBER` | VARCHAR(20) | UNIQUE, NOT NULL | Official University ID number. |
| `STUDENT_LAST_NAME` | VARCHAR(100) | NOT NULL | Student's last name. |
| `STUDENT_FIRST_NAME` | VARCHAR(100) | NOT NULL | Student's first name. |
| `STUDENT_COLLEGE` | VARCHAR(150) | NOT NULL | College the student belongs to. |
| `STUDENT_STATUS` | BOOLEAN | Default 1 | True = Active, False = Inactive/Blocked. |
| *Other fields:* | *Includes Middle Name, Suffix, Course, Email, Contact No, Timestamps.* | | |

#### Table: EQUIPMENT
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `EQUIPMENT_ID` | SERIAL | PK, Auto_Increment | Unique ID for equipment type. |
| `EQUIPMENT_NAME` | VARCHAR(100) | NOT NULL | Name (e.g., Laptop). |
| `EQUIPMENT_CATEGORY` | VARCHAR(50) | NOT NULL | Type/Category. |
| `EQUIPMENT_QUANTITY` | INT | NOT NULL | Total units available. |

#### Table: EQUIPMENT_UNIT
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `EUNIT_ID` | SERIAL | PK, Auto_Increment | Unique ID for specific unit. |
| `EQUIPMENT_ID` | INT | FK -> Equipment | Links to equipment type. |
| `EUNIT_SERIAL_NO` | VARCHAR(20) | NOT NULL, UNIQUE | Physical serial number. |
| `EUNIT_CONDITION` | BOOLEAN | NOT NULL | True = Good, False = Damaged. |
| `EUNIT_STATUS` | VARCHAR(50) | NOT NULL | 'available', 'borrowed', etc. |

#### Table: TRANSACTION
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `TRANSACTION_ID` | SERIAL | PK, Auto_Increment | Unique ID. |
| `STUDENT_ID` | INT | FK -> Student | Borrower. |
| `USER_ID` | INT | FK -> User | Staff handling transaction. |
| `TRANSACTION_CODE` | VARCHAR(100) | UNIQUE | System generated code. |
| `TRANSACTION_DATE_BORROWED` | TIMESTAMP | NOT NULL | Date borrowed. |

#### Table: BORROW_RECORD
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `BORROW_ID` | SERIAL | PK, Auto_Increment | Unique ID. |
| `TRANSACTION_ID` | INT | FK -> Transaction | Parent transaction. |
| `EUNIT_ID` | INT | FK -> Equipment_Unit | Specific item borrowed. |
| `BORROW_DUE_DATE` | TIMESTAMP | NOT NULL | Expected return. |
| `BORROW_STATUS` | VARCHAR(20) | NOT NULL | 'borrowed', 'returned', 'overdue'. |

#### Table: AUDIT_LOG
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
* **RBAC:** Strict separation between `admin_role` and `support_admin_role`.
* **Security:** Passwords hashed with bcrypt; SSL/TLS for connections; PII encrypted.
* **Indexing:** Implicit indexing on PKs and Unique columns; Explicit indexing on all Foreign Keys.

### SQL Schema Construction

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