using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DataSenseAPI.Domain.Models;
using System.Text.Json;

namespace DataSenseAPI.Infrastructure.AppDb
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
        public DbSet<Conversation> Conversations => Set<Conversation>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<RequestLog> RequestLogs => Set<RequestLog>();
        public DbSet<PricingRecord> PricingRecords => Set<PricingRecord>();
        public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
        public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
        public DbSet<UsageRequest> UsageRequests => Set<UsageRequest>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<Menu> Menus => Set<Menu>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApiKey - maps to existing api_keys table
            builder.Entity<ApiKey>(entity =>
            {
                entity.ToTable("api_keys");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("api_key_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.KeyHash).HasColumnName("key_hash");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.ExpiresAt).HasColumnName("last_used_at");
                entity.HasIndex(e => e.KeyHash);
                entity.HasIndex(e => e.UserId);
                entity.Property(e => e.UserMetadata).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));
            });

            // Configure Conversation - maps to existing conversations table
            builder.Entity<Conversation>(entity =>
            {
                entity.ToTable("conversations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("conversation_id");
                entity.Property(e => e.ApiKeyId).HasColumnName("api_key_id");
                entity.Property(e => e.ProjectId).HasColumnName("project_id");
                entity.HasIndex(e => e.ApiKeyId).HasDatabaseName("idx_conversations_api_key");
                entity.HasIndex(e => e.ProjectId).HasDatabaseName("idx_conversations_project_id");
                // Note: UserId, ExternalUserId, Type, PlatformType are new fields not in original table
                // They will be added via migration if needed
            });

            // Configure ChatMessage - maps to existing messages table
            builder.Entity<ChatMessage>(entity =>
            {
                entity.ToTable("messages");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("message_id");
                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
                entity.Property(e => e.Role).HasColumnName("role");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.Timestamp).HasColumnName("created_at");
                entity.HasIndex(e => e.ConversationId).HasDatabaseName("idx_messages_conversation");
                entity.Property(e => e.Metadata).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));
            });

            // Configure RequestLog - NEW table (different from audit_logs)
            builder.Entity<RequestLog>(entity =>
            {
                entity.ToTable("request_logs");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Timestamp);
                entity.Property(e => e.Metadata).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));
            });

            // Configure PricingRecord - NEW table
            builder.Entity<PricingRecord>(entity =>
            {
                entity.ToTable("pricing_records");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.Date });
            });

            // Configure SubscriptionPlan - maps to existing subscription_plans table
            builder.Entity<SubscriptionPlan>(entity =>
            {
                entity.ToTable("subscription_plans");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("plan_id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.MonthlyPrice).HasColumnName("monthly_price");
                entity.Property(e => e.MonthlyRequestLimit).HasColumnName("request_limit_per_month");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Ignore(e => e.Features); // Not in original table
                entity.Ignore(e => e.UpdatedAt); // Not in original table
                entity.HasIndex(e => e.Name);
            });

            // Configure UserSubscription - maps to existing user_subscriptions table
            builder.Entity<UserSubscription>(entity =>
            {
                entity.ToTable("user_subscriptions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("subscription_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.SubscriptionPlanId).HasColumnName("plan_id");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.EndDate).HasColumnName("end_date");
                // Note: status in DB is TEXT, IsActive maps to checking if status = 'active'
                entity.Ignore(e => e.IsActive); // Map manually or compute from status
                entity.Property(e => e.UsedRequestsThisMonth).HasColumnName("requests_used");
                entity.Ignore(e => e.LastResetDate); // Not in original table
                entity.HasIndex(e => e.UserId).HasDatabaseName("idx_user_subscriptions_user_id");
            });

            // Configure UsageRequest - maps to existing usage_requests table (note: simplified mapping)
            builder.Entity<UsageRequest>(entity =>
            {
                entity.ToTable("usage_requests");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("request_id");
                entity.Property(e => e.ApiKeyId).HasColumnName("api_key_id");
                entity.Property(e => e.Endpoint).HasColumnName("endpoint");
                entity.Property(e => e.Timestamp).HasColumnName("created_at");
                entity.Ignore(e => e.UserId); // Not directly in original table (linked via api_key)
                entity.Ignore(e => e.RequestType); // Not in original table
                entity.Ignore(e => e.StatusCode); // Not in original table
                entity.Ignore(e => e.ProcessingTimeMs); // Original has duration_ms but different name
                entity.Ignore(e => e.Metadata); // Not in original table
                entity.HasIndex(e => e.ApiKeyId).HasDatabaseName("idx_usage_requests_api_key");
                entity.HasIndex(e => e.Timestamp).HasDatabaseName("idx_usage_requests_created_at");
                // Note: Original table is partitioned and has different structure
                // This is a simplified mapping for EF Core access
            });

            // Configure RefreshToken - NEW table
            builder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("refresh_tokens");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Token);
                entity.HasIndex(e => new { e.UserId, e.IsRevoked });
            });

            // Configure Project - NEW table for project classification and project key
            builder.Entity<Project>(entity =>
            {
                entity.ToTable("projects");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("project_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.MessageChannel).HasColumnName("message_channel");
                entity.Property(e => e.ChannelNumber).HasColumnName("channel_number");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.Property(e => e.ProjectKeyHash).HasColumnName("project_key_hash");
                entity.HasIndex(e => e.UserId).HasDatabaseName("idx_projects_user_id");
                entity.HasIndex(e => e.ProjectKeyHash).HasDatabaseName("idx_projects_project_key_hash");
                entity.HasIndex(e => new { e.UserId, e.IsActive }).HasDatabaseName("idx_projects_user_active");
            });

            // Configure Menu - NEW table
            builder.Entity<Menu>(entity =>
            {
                entity.ToTable("menus");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.DisplayName).HasColumnName("display_name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.Icon).HasColumnName("icon");
                entity.Property(e => e.Url).HasColumnName("url");
                entity.Property(e => e.ParentId).HasColumnName("parent_id");
                entity.Property(e => e.Order).HasColumnName("order_index");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.HasIndex(e => e.ParentId).HasDatabaseName("idx_menus_parent_id");
            });

            // Configure RolePermission - NEW table
            builder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("role_permissions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.MenuId).HasColumnName("menu_id");
                entity.Property(e => e.CanView).HasColumnName("can_view");
                entity.Property(e => e.CanCreate).HasColumnName("can_create");
                entity.Property(e => e.CanEdit).HasColumnName("can_edit");
                entity.Property(e => e.CanDelete).HasColumnName("can_delete");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.HasIndex(e => e.RoleId).HasDatabaseName("idx_role_permissions_role_id");
                entity.HasIndex(e => e.MenuId).HasDatabaseName("idx_role_permissions_menu_id");
            });
        }
    }
}


