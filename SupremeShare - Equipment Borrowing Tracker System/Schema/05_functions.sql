-- ============================================
-- Stored Functions
-- ============================================

-- 1. fn_get_available_stock
CREATE OR REPLACE FUNCTION fn_get_available_stock(p_equipment_id INT)
RETURNS INT AS $$
DECLARE
    v_current_stock INT;
BEGIN
    SELECT equipment_quantity INTO v_current_stock
    FROM equipment
    WHERE equipment_id = p_equipment_id;
    
    RETURN COALESCE(v_current_stock, 0);
END;
$$ LANGUAGE plpgsql;

-- 2. fn_calculate_days_overdue
CREATE OR REPLACE FUNCTION fn_calculate_days_overdue(p_borrow_id INT)
RETURNS INT AS $$
DECLARE
    v_due_date TIMESTAMP;
    v_return_date TIMESTAMP;
    v_days_overdue INT;
BEGIN
    SELECT borrow_due_date, borrow_date_returned
    INTO v_due_date, v_return_date
    FROM borrow_record
    WHERE borrow_id = p_borrow_id;
    
    IF v_return_date IS NULL AND v_due_date < NOW() THEN
        v_days_overdue := EXTRACT(DAY FROM (NOW() - v_due_date));
        RETURN v_days_overdue;
    ELSE
        RETURN 0;
    END IF;
END;
$$ LANGUAGE plpgsql;


-- 3. fn_check_student_eligibility
CREATE OR REPLACE FUNCTION fn_check_student_eligibility(p_student_id INT)
RETURNS BOOLEAN AS $$
DECLARE
    v_status VARCHAR(20);
BEGIN
    SELECT student_status INTO v_status
    FROM student
    WHERE student_id = p_student_id;
    
    IF v_status = 'active' THEN
        RETURN TRUE;
    ELSE
        RETURN FALSE;
    END IF;
END;
$$ LANGUAGE plpgsql;
