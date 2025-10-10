-- Diagnostic and Fix Script for Azure SQL Database Login Issues
-- Run this in Azure Portal Query Editor or SSMS connected to your Azure SQL Database

-- 1. Check if AspNetUsers table exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUsers')
BEGIN
    PRINT 'AspNetUsers table exists'
    SELECT COUNT(*) as UserCount FROM AspNetUsers
END
ELSE
BEGIN
    PRINT 'ERROR: AspNetUsers table does NOT exist - migrations need to be run!'
END

-- 2. Check if AspNetRoles table exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetRoles')
BEGIN
    PRINT 'AspNetRoles table exists'
    SELECT COUNT(*) as RoleCount FROM AspNetRoles
END
ELSE
BEGIN
    PRINT 'ERROR: AspNetRoles table does NOT exist - migrations need to be run!'
END

-- 3. List all users (if table exists)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUsers')
BEGIN
    PRINT 'Current users in database:'
    SELECT Id, UserName, Email, EmailConfirmed, FirstName, LastName
    FROM AspNetUsers
END

-- 4. List all roles (if table exists)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetRoles')
BEGIN
    PRINT 'Current roles in database:'
    SELECT Id, Name FROM AspNetRoles
END

-- 5. Check user-role assignments
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUserRoles')
BEGIN
    PRINT 'User-Role assignments:'
    SELECT
        u.UserName,
        r.Name as RoleName
    FROM AspNetUserRoles ur
    JOIN AspNetUsers u ON ur.UserId = u.Id
    JOIN AspNetRoles r ON ur.RoleId = r.Id
END

-- If tables don't exist, you need to run migrations first!
-- If tables exist but no users, the admin user creation might have failed