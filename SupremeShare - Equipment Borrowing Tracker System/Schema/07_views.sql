-- ============================================
-- Security Views
-- ============================================

-- 1. transaction_borrowing_info_view
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
FROM "user";
