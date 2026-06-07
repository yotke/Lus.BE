using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addfieldmanagertoprojects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectManager",
                table: "ProjectTemplate",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectManager",
                table: "ProjectTemplate");
        }
    }
}
