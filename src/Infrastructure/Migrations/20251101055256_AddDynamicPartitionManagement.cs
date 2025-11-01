using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataSenseAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicPartitionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create function to ensure partition exists for a given date
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION ensure_usage_requests_partition(partition_date TIMESTAMPTZ DEFAULT NOW())
RETURNS VOID AS $$
DECLARE
    start_month DATE;
    end_month DATE;
    part_name TEXT;
BEGIN
    -- Get the first day of the month for the given date
    start_month := date_trunc('month', partition_date)::DATE;
    end_month := (start_month + INTERVAL '1 month')::DATE;
    part_name := format('usage_requests_%s_%s', to_char(start_month, 'YYYY'), to_char(start_month, 'MM'));
    
    -- Create partition if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM pg_class WHERE relname = part_name
    ) THEN
        EXECUTE format('CREATE TABLE IF NOT EXISTS %I PARTITION OF usage_requests FOR VALUES FROM (%L) TO (%L);', 
            part_name, start_month, end_month);
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Create function to ensure partition for current and next month (proactive)
CREATE OR REPLACE FUNCTION ensure_usage_requests_partitions()
RETURNS VOID AS $$
BEGIN
    -- Ensure partition for current month
    PERFORM ensure_usage_requests_partition(NOW());
    
    -- Ensure partition for next month
    PERFORM ensure_usage_requests_partition(NOW() + INTERVAL '1 month');
END;
$$ LANGUAGE plpgsql;

-- Create trigger function to automatically create partition before insert
CREATE OR REPLACE FUNCTION usage_requests_partition_trigger()
RETURNS TRIGGER AS $$
BEGIN
    -- Ensure partition exists for the created_at value
    PERFORM ensure_usage_requests_partition(COALESCE(NEW.created_at, NOW()));
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger (if table exists, otherwise it will be created when table is created)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'usage_requests') THEN
        DROP TRIGGER IF EXISTS usage_requests_before_insert_trigger ON usage_requests;
        CREATE TRIGGER usage_requests_before_insert_trigger
            BEFORE INSERT ON usage_requests
            FOR EACH ROW
            EXECUTE FUNCTION usage_requests_partition_trigger();
        
        -- Ensure partitions for current and next month exist now
        PERFORM ensure_usage_requests_partitions();
    END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop trigger
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS usage_requests_before_insert_trigger ON usage_requests;
");

            // Drop functions
            migrationBuilder.Sql(@"
DROP FUNCTION IF EXISTS usage_requests_partition_trigger();
DROP FUNCTION IF EXISTS ensure_usage_requests_partitions();
DROP FUNCTION IF EXISTS ensure_usage_requests_partition(TIMESTAMPTZ);
");
        }
    }
}
