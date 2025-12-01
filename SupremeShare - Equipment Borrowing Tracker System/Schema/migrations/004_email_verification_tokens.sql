-- Migration: Email verification system with unique email constraint
-- Purpose: Store pending email verification tokens and enforce unique emails per user
-- Date: 2025-12-01

-- ===========================================
-- PART 1: Email Verification Tokens Table
-- ===========================================

-- Create email verification tokens table
CREATE TABLE IF NOT EXISTS email_verification_tokens (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES "USER"(user_id) ON DELETE CASCADE,
    token VARCHAR(100) NOT NULL UNIQUE,
    new_email VARCHAR(100) NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    is_used BOOLEAN DEFAULT FALSE
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_email_verification_user_id ON email_verification_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_email_verification_token ON email_verification_tokens(token);
CREATE INDEX IF NOT EXISTS idx_email_verification_expires ON email_verification_tokens(expires_at);

-- Add comments for documentation
COMMENT ON TABLE email_verification_tokens IS 'Stores pending email verification tokens for email change requests';
COMMENT ON COLUMN email_verification_tokens.token IS 'Unique verification token sent via email';
COMMENT ON COLUMN email_verification_tokens.new_email IS 'The new email address to be verified';
COMMENT ON COLUMN email_verification_tokens.expires_at IS 'Token expiration timestamp (24 hours from creation)';
COMMENT ON COLUMN email_verification_tokens.is_used IS 'Whether the token has been used for verification';

-- ===========================================
-- PART 2: Unique Email Constraint on USER table
-- ===========================================

-- Add unique constraint on email (allows NULL - users without email)
-- This ensures each user has their own unique email address
ALTER TABLE "USER" 
DROP CONSTRAINT IF EXISTS user_email_unique;

ALTER TABLE "USER" 
ADD CONSTRAINT user_email_unique UNIQUE (user_email);

-- Add index for faster email lookups
CREATE INDEX IF NOT EXISTS idx_user_email ON "USER"(user_email) WHERE user_email IS NOT NULL;

-- Add comment for documentation
COMMENT ON CONSTRAINT user_email_unique ON "USER" IS 'Ensures each user has a unique email address for proper notification delivery';

-- ===========================================
-- Cleanup job (run periodically if needed)
-- ===========================================
-- DELETE FROM email_verification_tokens WHERE expires_at < CURRENT_TIMESTAMP OR is_used = TRUE;
