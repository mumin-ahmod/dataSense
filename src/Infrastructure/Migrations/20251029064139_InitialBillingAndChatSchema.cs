using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialBillingAndChatSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            // Ensure pgcrypto for gen_random_uuid
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

            migrationBuilder.Sql(@"
-- 2) SUBSCRIPTION PLANS
CREATE TABLE IF NOT EXISTS subscription_plans (
    plan_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    description TEXT,
    monthly_price NUMERIC(10,2) NOT NULL,
    request_limit_per_month BIGINT NOT NULL,
    overage_price_per_request NUMERIC(10,4) DEFAULT 0.0,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_subscription_plans_active ON subscription_plans(is_active);

-- 3) USER SUBSCRIPTIONS
CREATE TABLE IF NOT EXISTS user_subscriptions (
    subscription_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id VARCHAR(450) NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE,
    plan_id UUID NOT NULL REFERENCES subscription_plans(plan_id),
    start_date TIMESTAMPTZ DEFAULT NOW(),
    end_date TIMESTAMPTZ,
    next_billing_date TIMESTAMPTZ,
    status TEXT CHECK (status IN ('active','expired','canceled','paused')) DEFAULT 'active',
    auto_renew BOOLEAN DEFAULT TRUE,
    requests_used BIGINT DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_user_subscriptions_user_id ON user_subscriptions(user_id);
CREATE INDEX IF NOT EXISTS idx_user_subscriptions_status ON user_subscriptions(status);
            CREATE INDEX IF NOT EXISTS idx_user_subscriptions_next_billing ON user_subscriptions(next_billing_date);

            -- 1) API KEYS (after user_subscriptions exists)
            CREATE TABLE IF NOT EXISTS api_keys (
                api_key_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                user_id VARCHAR(450) NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE,
                subscription_id UUID REFERENCES user_subscriptions(subscription_id),
                key_hash TEXT UNIQUE NOT NULL,
                name TEXT NOT NULL,
                is_active BOOLEAN DEFAULT TRUE,
                created_at TIMESTAMPTZ DEFAULT NOW(),
                last_used_at TIMESTAMPTZ
            );
            CREATE INDEX IF NOT EXISTS idx_api_keys_user_id ON api_keys(user_id);
            CREATE INDEX IF NOT EXISTS idx_api_keys_subscription_id ON api_keys(subscription_id);
            CREATE INDEX IF NOT EXISTS idx_api_keys_is_active ON api_keys(is_active);
            CREATE INDEX IF NOT EXISTS idx_api_keys_key_hash_hash ON api_keys USING hash (key_hash);

-- 4) PAYMENTS
CREATE TABLE IF NOT EXISTS payments (
    payment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id VARCHAR(450) NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE,
    subscription_id UUID NOT NULL REFERENCES user_subscriptions(subscription_id),
    payment_provider TEXT,
    transaction_id TEXT UNIQUE,
    amount NUMERIC(10,2) NOT NULL,
    currency TEXT DEFAULT 'USD',
    status TEXT CHECK (status IN ('pending','completed','failed','refunded')) DEFAULT 'pending',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_payments_user_id ON payments(user_id);
CREATE INDEX IF NOT EXISTS idx_payments_subscription_id ON payments(subscription_id);
CREATE INDEX IF NOT EXISTS idx_payments_status ON payments(status);

-- 5) INVOICES
CREATE TABLE IF NOT EXISTS invoices (
    invoice_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subscription_id UUID NOT NULL REFERENCES user_subscriptions(subscription_id),
    user_id VARCHAR(450) NOT NULL REFERENCES ""AspNetUsers""(""Id""),
    invoice_number TEXT UNIQUE NOT NULL,
    period_start TIMESTAMPTZ,
    period_end TIMESTAMPTZ,
    total_amount NUMERIC(10,2) NOT NULL,
    currency TEXT DEFAULT 'USD',
    payment_status TEXT CHECK (payment_status IN ('unpaid','paid','failed','refunded')) DEFAULT 'unpaid',
    created_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_invoices_user_id ON invoices(user_id);
CREATE INDEX IF NOT EXISTS idx_invoices_subscription_id ON invoices(subscription_id);
CREATE INDEX IF NOT EXISTS idx_invoices_created_at ON invoices(created_at DESC);

-- 6) USAGE REQUESTS (PARTITIONED BY RANGE created_at)
CREATE TABLE IF NOT EXISTS usage_requests (
    request_id UUID NOT NULL DEFAULT gen_random_uuid(),
    api_key_id UUID NOT NULL REFERENCES api_keys(api_key_id),
    endpoint TEXT NOT NULL,
    tokens_used INT DEFAULT 0,
    duration_ms INT,
    status TEXT CHECK (status IN ('success','error','timeout')) DEFAULT 'success',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    PRIMARY KEY (request_id, created_at)
) PARTITION BY RANGE (created_at);
CREATE INDEX IF NOT EXISTS idx_usage_requests_api_key ON usage_requests(api_key_id);
CREATE INDEX IF NOT EXISTS idx_usage_requests_created_at ON usage_requests(created_at DESC);
-- Example current-month partition (adjust per deployment)
DO $$
DECLARE start_month date := date_trunc('month', now())::date;
DECLARE end_month date := (start_month + interval '1 month')::date;
DECLARE part_name text := format('usage_requests_%s_%s', to_char(start_month, 'YYYY'), to_char(start_month, 'MM'));
BEGIN
    EXECUTE format('CREATE TABLE IF NOT EXISTS %I PARTITION OF usage_requests FOR VALUES FROM (%L) TO (%L);', part_name, start_month, end_month);
END $$;

-- 7) BILLING EVENTS
CREATE TABLE IF NOT EXISTS billing_events (
    billing_event_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subscription_id UUID NOT NULL REFERENCES user_subscriptions(subscription_id),
    api_key_id UUID REFERENCES api_keys(api_key_id),
    request_id UUID,
    tokens_used INT,
    estimated_cost NUMERIC(10,4),
    event_type TEXT CHECK (event_type IN ('request','overage','subscription','refund')),
    idempotency_key TEXT UNIQUE NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_billing_events_subscription_id ON billing_events(subscription_id);
CREATE INDEX IF NOT EXISTS idx_billing_events_created_at ON billing_events(created_at DESC);

-- 8) CONVERSATIONS
CREATE TABLE IF NOT EXISTS conversations (
    conversation_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    api_key_id UUID NOT NULL REFERENCES api_keys(api_key_id),
    title TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    last_activity TIMESTAMPTZ DEFAULT NOW(),
    is_active BOOLEAN DEFAULT TRUE
);
CREATE INDEX IF NOT EXISTS idx_conversations_api_key ON conversations(api_key_id);
CREATE INDEX IF NOT EXISTS idx_conversations_last_activity ON conversations(last_activity DESC);

-- 9) MESSAGES (PARTITIONED BY RANGE created_at)
CREATE TABLE IF NOT EXISTS messages (
    message_id UUID NOT NULL DEFAULT gen_random_uuid(),
    conversation_id UUID NOT NULL REFERENCES conversations(conversation_id) ON DELETE CASCADE,
    role TEXT CHECK (role IN ('user','assistant','system')) NOT NULL,
    content TEXT NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    PRIMARY KEY (message_id, created_at)
) PARTITION BY RANGE (created_at);
CREATE INDEX IF NOT EXISTS idx_messages_conversation ON messages(conversation_id);
CREATE INDEX IF NOT EXISTS idx_messages_created_at ON messages(created_at DESC);
-- Example current-month partition
DO $$
DECLARE start_month date := date_trunc('month', now())::date;
DECLARE end_month date := (start_month + interval '1 month')::date;
DECLARE part_name text := format('messages_%s_%s', to_char(start_month, 'YYYY'), to_char(start_month, 'MM'));
BEGIN
    EXECUTE format('CREATE TABLE IF NOT EXISTS %I PARTITION OF messages FOR VALUES FROM (%L) TO (%L);', part_name, start_month, end_month);
END $$;

-- 10) AUDIT LOGS (PARTITIONED BY RANGE created_at)
CREATE TABLE IF NOT EXISTS audit_logs (
    audit_id UUID NOT NULL DEFAULT gen_random_uuid(),
    api_key_id UUID NOT NULL REFERENCES api_keys(api_key_id),
    request_id UUID,
    event_type TEXT CHECK (event_type IN ('generate_sql','validate_sql','interpretation','error')),
    details JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    PRIMARY KEY (audit_id, created_at)
) PARTITION BY RANGE (created_at);
CREATE INDEX IF NOT EXISTS idx_audit_logs_api_key ON audit_logs(api_key_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_event_type ON audit_logs(event_type);
CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at ON audit_logs(created_at DESC);
-- Example current-month partition
DO $$
DECLARE start_month date := date_trunc('month', now())::date;
DECLARE end_month date := (start_month + interval '1 month')::date;
DECLARE part_name text := format('audit_logs_%s_%s', to_char(start_month, 'YYYY'), to_char(start_month, 'MM'));
BEGIN
    EXECUTE format('CREATE TABLE IF NOT EXISTS %I PARTITION OF audit_logs FOR VALUES FROM (%L) TO (%L);', part_name, start_month, end_month);
END $$;

-- 12) MATERIALIZED VIEW (optional)
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_daily_usage AS
SELECT
    api_key_id,
    DATE(created_at) AS usage_date,
    COUNT(*) AS total_requests,
    SUM(tokens_used) AS total_tokens
FROM usage_requests
GROUP BY api_key_id, DATE(created_at);
CREATE INDEX IF NOT EXISTS idx_mv_daily_usage_date ON mv_daily_usage(usage_date DESC);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop objects created via SQL (in reverse dependency order)
            migrationBuilder.Sql(@"
DROP MATERIALIZED VIEW IF EXISTS mv_daily_usage;

-- Drop partitions first (if exist) using dynamic SQL
DO $$
DECLARE r record;
BEGIN
  FOR r IN (
    SELECT inhrelid::regclass AS part_name FROM pg_inherits
    WHERE inhparent = 'usage_requests'::regclass
  ) LOOP
    EXECUTE format('DROP TABLE IF EXISTS %s;', r.part_name);
  END LOOP;
END $$;

DO $$
DECLARE r record;
BEGIN
  FOR r IN (
    SELECT inhrelid::regclass AS part_name FROM pg_inherits
    WHERE inhparent = 'messages'::regclass
  ) LOOP
    EXECUTE format('DROP TABLE IF EXISTS %s;', r.part_name);
  END LOOP;
END $$;

DO $$
DECLARE r record;
BEGIN
  FOR r IN (
    SELECT inhrelid::regclass AS part_name FROM pg_inherits
    WHERE inhparent = 'audit_logs'::regclass
  ) LOOP
    EXECUTE format('DROP TABLE IF EXISTS %s;', r.part_name);
  END LOOP;
END $$;

DROP TABLE IF EXISTS billing_events;
DROP TABLE IF EXISTS messages;
DROP TABLE IF EXISTS conversations;
DROP TABLE IF EXISTS audit_logs;
DROP TABLE IF EXISTS usage_requests;
DROP TABLE IF EXISTS invoices;
DROP TABLE IF EXISTS payments;
DROP TABLE IF EXISTS user_subscriptions;
DROP TABLE IF EXISTS subscription_plans;
DROP TABLE IF EXISTS api_keys;

");
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
