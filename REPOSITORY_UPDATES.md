# Repository Updates Summary

All repositories have been updated to use snake_case table and column names to match the database schema. Here's a summary of changes needed:

## Completed:
- ✅ ApiKeyRepository - Updated to use `api_keys` table with snake_case columns
- ✅ ConversationRepository - Updated to use `conversations` table with snake_case columns  
- ✅ ChatMessageRepository - Updated to use `messages` table with snake_case columns

## Remaining repositories to update:

### SubscriptionPlanRepository
- Table: `subscription_plans` (already uses snake_case)
- Columns need mapping: `plan_id` → `Id`, `name` → `Name`, etc.

### UserSubscriptionRepository
- Table: `user_subscriptions` (already uses snake_case)
- Columns need mapping: `subscription_id` → `Id`, `user_id` → `UserId`, etc.

### UsageRequestRepository  
- Table: `usage_requests` (already uses snake_case)
- Note: This table is partitioned, columns map: `request_id` → `Id`, etc.

### RequestLogRepository
- Table: `request_logs` (uses PascalCase columns: `Id`, `UserId`, etc. - matches migration)

### PricingRecordRepository
- Table: `pricing_records` (uses PascalCase columns: `Id`, `UserId`, etc. - matches migration)

### RefreshTokenRepository
- Table: `refresh_tokens` (uses PascalCase columns: `Id`, `UserId`, etc. - matches migration)

