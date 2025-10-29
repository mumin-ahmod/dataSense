using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendedChatAndPlatformIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Alter conversations table - add conversation_type and initiator_type
            migrationBuilder.Sql(@"
ALTER TABLE conversations
ADD COLUMN IF NOT EXISTS conversation_type TEXT CHECK (conversation_type IN ('ai_chat', 'user_chat')) DEFAULT 'ai_chat',
ADD COLUMN IF NOT EXISTS initiator_type TEXT CHECK (initiator_type IN ('user', 'api_owner', 'external_user')) DEFAULT 'user';
");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS idx_conversations_type ON conversations(conversation_type);
CREATE INDEX IF NOT EXISTS idx_conversations_initiator ON conversations(initiator_type);
CREATE INDEX IF NOT EXISTS idx_conversations_api_key_type ON conversations(api_key_id, conversation_type);
");

            // 2. Create message_channels table and seed initial data
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS message_channels (
    channel_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT CHECK (name IN ('web','dashboard','telegram','whatsapp','api')) NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_message_channels_name ON message_channels(name);
");

            // Seed message_channels with initial values
            migrationBuilder.Sql(@"
INSERT INTO message_channels (name, description) VALUES
    ('web', 'Web interface'),
    ('dashboard', 'Dashboard interface'),
    ('telegram', 'Telegram bot'),
    ('whatsapp', 'WhatsApp integration'),
    ('api', 'API requests')
ON CONFLICT (name) DO NOTHING;
");

            // 3. Alter messages table - add sender and channel columns
            migrationBuilder.Sql(@"
ALTER TABLE messages
ADD COLUMN IF NOT EXISTS sender_user_id TEXT REFERENCES ""AspNetUsers""(""Id""),
ADD COLUMN IF NOT EXISTS sender_external_user_id UUID,
ADD COLUMN IF NOT EXISTS channel_id UUID REFERENCES message_channels(channel_id);
");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS idx_messages_sender_user ON messages(sender_user_id);
CREATE INDEX IF NOT EXISTS idx_messages_sender_external ON messages(sender_external_user_id);
CREATE INDEX IF NOT EXISTS idx_messages_channel ON messages(channel_id);
CREATE INDEX IF NOT EXISTS idx_messages_conv_created ON messages(conversation_id, created_at DESC);
");

            // 4. Create external_users table
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS external_users (
    external_user_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    api_key_id UUID NOT NULL REFERENCES api_keys(api_key_id) ON DELETE CASCADE,
    platform TEXT CHECK (platform IN ('telegram','whatsapp','web')) DEFAULT 'web',
    platform_user_id TEXT NOT NULL,
    display_name TEXT,
    is_blocked BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    last_seen_at TIMESTAMPTZ
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_external_user_platform_uid 
    ON external_users(api_key_id, platform, platform_user_id);
CREATE INDEX IF NOT EXISTS idx_external_users_api_key ON external_users(api_key_id);
CREATE INDEX IF NOT EXISTS idx_external_users_platform ON external_users(platform);
");

            // 5. Add foreign key constraint for sender_external_user_id in messages
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_messages_external_user'
    ) THEN
        ALTER TABLE messages
        ADD CONSTRAINT FK_messages_external_user 
            FOREIGN KEY (sender_external_user_id) 
            REFERENCES external_users(external_user_id) 
            ON DELETE SET NULL;
    END IF;
END $$;
");

            // 6. Create conversation_participants table
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS conversation_participants (
    participant_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id UUID NOT NULL REFERENCES conversations(conversation_id) ON DELETE CASCADE,
    user_id TEXT REFERENCES ""AspNetUsers""(""Id""),
    external_user_id UUID REFERENCES external_users(external_user_id) ON DELETE CASCADE,
    role TEXT CHECK (role IN ('api_owner','external_user','ai_assistant')) NOT NULL,
    joined_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_conv_participants_conv ON conversation_participants(conversation_id);
CREATE INDEX IF NOT EXISTS idx_conv_participants_user ON conversation_participants(user_id);
CREATE INDEX IF NOT EXISTS idx_conv_participants_external ON conversation_participants(external_user_id);
");

            // 7. Create platform_configs table
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS platform_configs (
    config_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE,
    api_key_id UUID NOT NULL REFERENCES api_keys(api_key_id),
    platform TEXT CHECK (platform IN ('telegram','whatsapp')) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    config_data JSONB,
    verified BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_platform_config_user_unique ON platform_configs(user_id);
CREATE INDEX IF NOT EXISTS idx_platform_config_api_key ON platform_configs(api_key_id);
CREATE INDEX IF NOT EXISTS idx_platform_config_platform ON platform_configs(platform);
CREATE INDEX IF NOT EXISTS idx_platform_config_active ON platform_configs(is_active) WHERE is_active = TRUE;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop in reverse order of dependencies
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS platform_configs CASCADE;
DROP TABLE IF EXISTS conversation_participants CASCADE;
DROP TABLE IF EXISTS external_users CASCADE;

-- Drop foreign key constraint on messages
ALTER TABLE messages DROP CONSTRAINT IF EXISTS FK_messages_external_user;

-- Drop columns from messages
ALTER TABLE messages 
DROP COLUMN IF EXISTS sender_user_id,
DROP COLUMN IF EXISTS sender_external_user_id,
DROP COLUMN IF EXISTS channel_id;

-- Drop indexes on messages
DROP INDEX IF EXISTS idx_messages_sender_user;
DROP INDEX IF EXISTS idx_messages_sender_external;
DROP INDEX IF EXISTS idx_messages_channel;
DROP INDEX IF EXISTS idx_messages_conv_created;

-- Drop message_channels
DROP TABLE IF EXISTS message_channels CASCADE;

-- Drop columns from conversations
ALTER TABLE conversations
DROP COLUMN IF EXISTS conversation_type,
DROP COLUMN IF EXISTS initiator_type;

-- Drop indexes on conversations
DROP INDEX IF EXISTS idx_conversations_type;
DROP INDEX IF EXISTS idx_conversations_initiator;
DROP INDEX IF EXISTS idx_conversations_api_key_type;
");
        }
    }
}

