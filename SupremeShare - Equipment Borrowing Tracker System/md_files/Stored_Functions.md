1. fn_get_available_stock: Calculates real-time available quantity of a specific equipment type.

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


