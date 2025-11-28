-- ============================================
-- Index Creation
-- ============================================

CREATE INDEX idx_equipment_unit_equipment_id ON EQUIPMENT_UNIT(EQUIPMENT_ID);
CREATE INDEX idx_transaction_student_id ON TRANSACTION(STUDENT_ID);
CREATE INDEX idx_transaction_user_id ON TRANSACTION(USER_ID);
CREATE INDEX idx_borrow_record_transaction_id ON BORROW_RECORD(TRANSACTION_ID);
CREATE INDEX idx_borrow_record_eunit_id ON BORROW_RECORD(EUNIT_ID);
CREATE INDEX idx_audit_log_user_id ON AUDIT_LOG(USER_ID);
