# Library API

ASP.NET Core API for library management.

## Setup
1. Run the app
2. Call `POST /api/auth/setup` 
3. Login with `admin@library.com` / `AdminPassword123!`

## Documentation
- **Scalar API Docs**
- Shows all endpoints, parameters, and responses
- Test API directly from the documentation

## What Works
- Register/login users
- Add/remove books (Clerk/Admin)
- Checkout/return books  
- Place/cancel holds
- Renew books (if no holds)
- View transaction history
- Notifications for due dates & available holds
- Cleanup expired holds

## Notes
- Uses SQL Server + JWT auth
- 3 roles: Member, Clerk, Admin
- Scalar for API documentation



- Uses SQL Server + JWT auth
- 3 roles: Member, Clerk, Admin
- Scalar for API documentation
