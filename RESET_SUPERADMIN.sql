USE [DBBudgetMaster]
GO
SET QUOTED_IDENTIFIER ON
GO

PRINT '============================================'
PRINT 'Deleting SuperAdmin user...'
PRINT '============================================'
PRINT ''

-- Delete from UserRoles first (foreign key)
DELETE FROM UserRoles 
WHERE UserId IN (SELECT Id FROM Users WHERE Email = 'superadmin@budgetmaster.com');

-- Delete the user
DELETE FROM Users 
WHERE Email = 'superadmin@budgetmaster.com';

PRINT '✅ SuperAdmin user deleted'
PRINT ''
PRINT 'Now restart your application.'
PRINT 'The SeedData will recreate the SuperAdmin with:'
PRINT 'Email: superadmin@budgetmaster.com'
PRINT 'Password: Admin@123'
PRINT ''
PRINT '============================================'

GO
