using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newfieldtoprojectTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkRate",
                table: "ProjectTemplate",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkRate",
                table: "ProjectTemplate");
        }
    }
}
