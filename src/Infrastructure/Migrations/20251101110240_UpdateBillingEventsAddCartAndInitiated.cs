using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataSenseAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBillingEventsAddCartAndInitiated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE billing_events
                DROP CONSTRAINT IF EXISTS billing_events_event_type_check;
                
                ALTER TABLE billing_events
                ADD CONSTRAINT billing_events_event_type_check 
                CHECK (event_type IN ('request','overage','subscription','refund','cart','initiated'));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE billing_events
                DROP CONSTRAINT IF EXISTS billing_events_event_type_check;
                
                ALTER TABLE billing_events
                ADD CONSTRAINT billing_events_event_type_check 
                CHECK (event_type IN ('request','overage','subscription','refund'));
            ");
        }
    }
}
