-- Migration: Create email_verification_tokens table
-- Purpose: Store pending email verification tokens for email change requests
-- Date: 2025-12-01

-- Create email verification tokens table
CREATE TABLE IF NOT EXISTS email_verification_tokens (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES "USER"(user_id) ON DELETE CASCADE,
    token VARCHAR(100) NOT NULL UNIQUE,
    new_email VARCHAR(100) NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    is_used BOOLEAN DEFAULT FALSE,
    
    -- Index for faster lookups
    CONSTRAINT email_verification_tokens_token_key UNIQUE (token)
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_email_verification_user_id ON email_verification_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_email_verification_token ON email_verification_tokens(token);
CREATE INDEX IF NOT EXISTS idx_email_verification_expires ON email_verification_tokens(expires_at);

-- Add comment for documentation
COMMENT ON TABLE email_verification_tokens IS 'Stores pending email verification tokens for email change requests';
COMMENT ON COLUMN email_verification_tokens.token IS 'Unique verification token sent via email';
COMMENT ON COLUMN email_verification_tokens.new_email IS 'The new email address to be verified';
COMMENT ON COLUMN email_verification_tokens.expires_at IS 'Token expiration timestamp (24 hours from creation)';
COMMENT ON COLUMN email_verification_tokens.is_used IS 'Whether the token has been used for verification';

-- Cleanup job: Delete expired tokens (run periodically)
-- DELETE FROM email_verification_tokens WHERE expires_at < CURRENT_TIMESTAMP OR is_used = TRUE;
