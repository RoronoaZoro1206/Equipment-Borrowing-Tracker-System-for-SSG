-- ============================================
-- Migration: Rehash Plaintext Passwords to BCrypt
-- Date: 2025-11-30
-- Description: Clean up users with plaintext passwords so the app can recreate them with BCrypt
-- ============================================

-- OPTION 1: Delete audit logs first, then delete users (RECOMMENDED FOR FRESH START)
-- This clears all audit history for the affected users

-- Step 1: Delete audit logs for users with plaintext passwords
DELETE FROM audit_log 
WHERE user_id IN (
    SELECT user_id FROM "USER" 
    WHERE user_hashed_password NOT LIKE '$2a$%' 
      AND user_hashed_password NOT LIKE '$2b$%' 
      AND user_hashed_password NOT LIKE '$2y$%'
);

-- Step 2: Delete users with plaintext passwords
DELETE FROM "USER" 
WHERE user_hashed_password NOT LIKE '$2a$%' 
  AND user_hashed_password NOT LIKE '$2b$%' 
  AND user_hashed_password NOT LIKE '$2y$%';

-- Step 3: Verify - should return 0 rows
SELECT COUNT(*) as remaining_plaintext_users FROM "USER" 
WHERE user_hashed_password NOT LIKE '$2a$%' 
  AND user_hashed_password NOT LIKE '$2b$%' 
  AND user_hashed_password NOT LIKE '$2y$%';

-- ============================================
-- After running this script:
-- 1. Restart your application
-- 2. AdminInitializer will recreate admin account with BCrypt hash
-- 3. SupportAdminInitializer will recreate support_admin with BCrypt hash
-- ============================================
