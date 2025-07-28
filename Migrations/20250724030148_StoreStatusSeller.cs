using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyDatHang.Migrations
{
    /// <inheritdoc />
    public partial class StoreStatusSeller : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StatusStoreSeller",
                table: "Stores",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusStoreSeller",
                table: "Stores");
        }
    }
}
