using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataSenseAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateUsageRecordsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS usage_records (
                    id TEXT PRIMARY KEY,
                    user_id TEXT NOT NULL,
                    api_key_id UUID REFERENCES api_keys(api_key_id),
                    request_type INTEGER NOT NULL,
                    request_count INTEGER NOT NULL,
                    request_left INTEGER NOT NULL,
                    date TIMESTAMPTZ NOT NULL,
                    CONSTRAINT usage_records_user_id_date_request_type_key UNIQUE (user_id, date, request_type)
                );

                CREATE INDEX IF NOT EXISTS ix_usage_records_user_id ON usage_records USING btree (user_id);
                CREATE INDEX IF NOT EXISTS ix_usage_records_api_key_id ON usage_records USING btree (api_key_id);
                CREATE INDEX IF NOT EXISTS ix_usage_records_date ON usage_records USING btree (date);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS usage_records;
            ");
        }
    }
}
