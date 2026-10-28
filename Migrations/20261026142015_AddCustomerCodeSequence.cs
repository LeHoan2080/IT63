using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSalesCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerCodeSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "CustomerCodeSequence");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "CustomerCodeSequence");
        }
    }
}
