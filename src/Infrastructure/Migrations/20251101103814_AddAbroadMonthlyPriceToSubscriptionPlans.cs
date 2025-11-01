using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataSenseAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbroadMonthlyPriceToSubscriptionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE subscription_plans 
                ADD COLUMN IF NOT EXISTS abroad_monthly_price NUMERIC(10,2);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE subscription_plans 
                DROP COLUMN IF EXISTS abroad_monthly_price;
            ");
        }
    }
}
