USE [DBBudgetMaster]
GO
SET QUOTED_IDENTIFIER ON
GO
UPDATE Users SET TwoFactorEnabled = 0, AuthenticatorKey = NULL WHERE Email = 'superadmin@budgetmaster.com';
GO
SELECT 'SUCCESS: 2FA Disabled' as Result;
GO
