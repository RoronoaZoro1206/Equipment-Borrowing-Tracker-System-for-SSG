-- ============================================
-- Role and Privilege Management
-- ============================================

-- Create roles
CREATE ROLE admin_role;
CREATE ROLE support_admin_role;

-- Create users and assign roles
CREATE ROLE admin_user LOGIN PASSWORD 'secure_admin_password';
GRANT admin_role TO admin_user;

CREATE ROLE support_user LOGIN PASSWORD 'secure_support_password';
GRANT support_admin_role TO support_user;

-- Grant privileges to admin_role
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO admin_role;

-- Grant privileges to support_admin_role
GRANT SELECT, INSERT, UPDATE ON student TO support_admin_role;
GRANT SELECT, INSERT, UPDATE ON transaction TO support_admin_role;
GRANT SELECT, INSERT, UPDATE ON borrow_record TO support_admin_role;

-- Audit schema access
GRANT USAGE ON SCHEMA audit TO admin_role;
REVOKE ALL ON SCHEMA audit FROM support_admin_role;
