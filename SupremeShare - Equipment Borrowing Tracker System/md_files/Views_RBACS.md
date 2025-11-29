1. transaction_borrowing_info_view: Displays simplified transaction details for admins, hiding sensitive metadata.

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

2. admin_user_management_view: Displays user profiles excluding passwords and audit timestamps.

CREATE OR REPLACE VIEW admin_user_management_view AS
SELECT
    user_id, user_username, user_last_name, user_first_name,
    user_middle_name, user_suffix, user_role, user_position,
    user_email, user_contact_no, user_status
FROM "USER";

RBAC Implementation (Sample) 
Roles: admin_role (Full Access), support_admin_role (Limited Access).
Schema Separation: Audit logs stored in a separate schema audit accessible only to admin_role.

CREATE ROLE admin_role;
CREATE ROLE support_admin_role;

-- Grant Full Access to Admin
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO admin_role;

-- Grant Limited Access to Support Admin
GRANT SELECT, INSERT, UPDATE ON student TO support_admin_role;
GRANT SELECT, INSERT, UPDATE ON transaction TO support_admin_role;
GRANT SELECT, INSERT, UPDATE ON borrow_record TO support_admin_role;
-- Support Admin cannot delete records or modify system configs