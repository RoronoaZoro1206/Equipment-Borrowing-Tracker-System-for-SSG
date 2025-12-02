-- Migration: Email verification system with unique email constraint
-- Purpose: Store pending email verification tokens and enforce unique emails per user
-- Date: 2025-12-01

-- ===========================================
-- PART 1: Email Verification Tokens Table
-- ===========================================

-- Create email verification tokens table
CREATE TABLE IF NOT EXISTS EMAIL_VERIFICATION_TOKEN (
    EVT_ID SERIAL PRIMARY KEY,
    USER_ID INTEGER NOT NULL REFERENCES "USER"(USER_ID) ON DELETE CASCADE,
    EVT_TOKEN VARCHAR(100) NOT NULL UNIQUE,
    EVT_NEW_EMAIL VARCHAR(100) NOT NULL,
    EVT_EXPIRES_AT TIMESTAMP WITH TIME ZONE NOT NULL,
    EVT_DATE_CREATED TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    EVT_IS_USED BOOLEAN DEFAULT FALSE
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_evt_user_id ON EMAIL_VERIFICATION_TOKEN(USER_ID);
CREATE INDEX IF NOT EXISTS idx_evt_token ON EMAIL_VERIFICATION_TOKEN(EVT_TOKEN);
CREATE INDEX IF NOT EXISTS idx_evt_expires ON EMAIL_VERIFICATION_TOKEN(EVT_EXPIRES_AT);

-- Add comments for documentation
COMMENT ON TABLE EMAIL_VERIFICATION_TOKEN IS 'Stores pending email verification tokens for email change requests';
COMMENT ON COLUMN EMAIL_VERIFICATION_TOKEN.EVT_TOKEN IS 'Unique verification token sent via email';
COMMENT ON COLUMN EMAIL_VERIFICATION_TOKEN.EVT_NEW_EMAIL IS 'The new email address to be verified';
COMMENT ON COLUMN EMAIL_VERIFICATION_TOKEN.EVT_EXPIRES_AT IS 'Token expiration timestamp (24 hours from creation)';
COMMENT ON COLUMN EMAIL_VERIFICATION_TOKEN.EVT_IS_USED IS 'Whether the token has been used for verification';

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
-- DELETE FROM EMAIL_VERIFICATION_TOKEN WHERE EVT_EXPIRES_AT < CURRENT_TIMESTAMP OR EVT_IS_USED = TRUE;
