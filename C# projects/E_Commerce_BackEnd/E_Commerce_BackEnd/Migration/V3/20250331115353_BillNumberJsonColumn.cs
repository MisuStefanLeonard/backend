using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce_BackEnd.Migration.V3
{
    /// <inheritdoc />
    public partial class BillNumberJsonColumn : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bill_number",
                table: "comenzi");

            migrationBuilder.AddColumn<string>(
                name: "numar_factura_json",
                table: "comenzi",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "numar_factura_json",
                table: "comenzi");

            migrationBuilder.AddColumn<string>(
                name: "bill_number",
                table: "comenzi",
                type: "varchar(75)",
                maxLength: 75,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
