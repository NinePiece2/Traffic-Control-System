using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Traffic_Control_System.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountApprovedColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AccountApproved",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountApproved",
                table: "Users");
        }
    }
}
