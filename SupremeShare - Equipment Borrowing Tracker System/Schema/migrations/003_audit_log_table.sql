-- Migration: Create audit_log table
-- Date: 2025-11-30
-- Description: Creates the audit_log table for tracking all system actions

-- Create audit_log table if it doesn't exist
CREATE TABLE IF NOT EXISTS audit_log (
    audit_id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES "USER"(user_id) ON DELETE SET NULL,
    audit_action VARCHAR(255) NOT NULL,
    audit_old_data TEXT,
    audit_new_data TEXT,
    audit_logged_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_audit_log_user_id ON audit_log(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_log_action ON audit_log(audit_action);
CREATE INDEX IF NOT EXISTS idx_audit_log_logged_at ON audit_log(audit_logged_at DESC);

-- Add comment to table
COMMENT ON TABLE audit_log IS 'Stores audit trail of all system actions for compliance and tracking';
COMMENT ON COLUMN audit_log.audit_id IS 'Primary key';
COMMENT ON COLUMN audit_log.user_id IS 'User who performed the action (NULL for system actions)';
COMMENT ON COLUMN audit_log.audit_action IS 'Description of the action performed';
COMMENT ON COLUMN audit_log.audit_old_data IS 'JSON representation of data before change';
COMMENT ON COLUMN audit_log.audit_new_data IS 'JSON representation of data after change';
COMMENT ON COLUMN audit_log.audit_logged_at IS 'Timestamp when the action was logged';
