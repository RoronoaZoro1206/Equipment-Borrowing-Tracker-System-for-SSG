-- ============================================
-- Stored Procedures
-- ============================================

-- 1. sp_process_borrow_transaction
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
    SELECT eunit_id INTO v_eunit_id
    FROM equipment_unit
    WHERE equipment_id = p_equipment_id AND eunit_status = 'available'
    LIMIT 1;
    
    IF v_eunit_id IS NULL THEN
        RAISE EXCEPTION 'No available units for this equipment ID';
    END IF;
    
    v_trans_code := 'TR-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || FLOOR(RANDOM() * 1000)::TEXT;
    
    INSERT INTO transaction (student_id, user_id, transaction_code, transaction_date_borrowed)
    VALUES (p_student_id, p_user_id, v_trans_code, NOW())
    RETURNING transaction_id INTO v_transaction_id;
    
    INSERT INTO borrow_record (transaction_id, eunit_id, borrow_due_date, borrow_status)
    VALUES (v_transaction_id, v_eunit_id, p_due_date, 'borrowed');
    
    UPDATE equipment_unit
    SET eunit_status = 'borrowed'
    WHERE eunit_id = v_eunit_id;
    
    UPDATE equipment
    SET equipment_quantity = equipment_quantity - 1
    WHERE equipment_id = p_equipment_id;
    
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'Transaction failed: %', SQLERRM;
        ROLLBACK;
END;
$$;


-- 2. sp_process_return_transaction
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
    SELECT b.eunit_id, u.equipment_id
    INTO v_eunit_id, v_equipment_id
    FROM borrow_record b
    JOIN equipment_unit u ON b.eunit_id = u.eunit_id
    WHERE b.borrow_id = p_borrow_id;
    
    IF p_return_condition = 'Good' THEN
        v_new_status := 'available';
    ELSE
        v_new_status := 'for review';
    END IF;
    
    UPDATE borrow_record
    SET borrow_date_returned = NOW(),
        borrow_return_condition = p_return_condition,
        borrow_status = 'returned'
    WHERE borrow_id = p_borrow_id;
    
    UPDATE equipment_unit
    SET eunit_status = v_new_status,
        eunit_condition = p_return_condition
    WHERE eunit_id = v_eunit_id;
    
    IF v_new_status = 'available' THEN
        UPDATE equipment
        SET equipment_quantity = equipment_quantity + 1
        WHERE equipment_id = v_equipment_id;
    END IF;
    
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'Return processing failed: %', SQLERRM;
        ROLLBACK;
END;
$$;

-- 3. sp_mark_item_lost
CREATE OR REPLACE PROCEDURE sp_mark_item_lost(p_borrow_id INT)
LANGUAGE plpgsql
AS $$
DECLARE
    v_eunit_id INT;
    v_student_id INT;
BEGIN
    SELECT eunit_id, student_id INTO v_eunit_id, v_student_id
    FROM borrow_record 
    JOIN transaction USING (transaction_id)
    WHERE borrow_id = p_borrow_id;
    
    UPDATE borrow_record 
    SET borrow_status = 'lost', 
        borrow_return_condition = 'Lost/Missing' 
    WHERE borrow_id = p_borrow_id;
    
    UPDATE equipment_unit 
    SET eunit_status = 'lost' 
    WHERE eunit_id = v_eunit_id;
    
    UPDATE student 
    SET student_status = 'blocked' 
    WHERE student_id = v_student_id;
    
EXCEPTION
    WHEN OTHERS THEN 
        ROLLBACK; 
        RAISE;
END;
$$;

-- 4. sp_update_overdue_statuses
CREATE OR REPLACE PROCEDURE sp_update_overdue_statuses()
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE borrow_record
    SET borrow_status = 'overdue'
    WHERE borrow_status = 'borrowed' AND borrow_due_date < NOW();
    
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
$$;
