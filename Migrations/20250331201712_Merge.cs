using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bili.Migrations
{
    /// <inheritdoc />
    public partial class Merge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Synonyms",
                table: "Dictionary");

            migrationBuilder.RenameColumn(
                name: "Translate",
                table: "Dictionary",
                newName: "Word");

            migrationBuilder.RenameColumn(
                name: "Term",
                table: "Dictionary",
                newName: "Translation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Word",
                table: "Dictionary",
                newName: "Translate");

            migrationBuilder.RenameColumn(
                name: "Translation",
                table: "Dictionary",
                newName: "Term");

            migrationBuilder.AddColumn<string>(
                name: "Synonyms",
                table: "Dictionary",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
