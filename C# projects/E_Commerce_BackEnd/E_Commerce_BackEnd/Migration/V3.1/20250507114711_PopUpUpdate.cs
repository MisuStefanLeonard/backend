using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce_BackEnd.Migration.V3._1
{
    /// <inheritdoc />
    public partial class PopUpUpdate : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "numar_factura_json",
                table: "comenzi",
                type: "json",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "json")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pop_ups",
                columns: table => new
                {
                    id_pop_up = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    descriere_pop_up = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    titlu_pop_up = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_voucher = table.Column<int>(type: "int", nullable: true),
                    is_active = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pop_ups", x => x.id_pop_up);
                    table.ForeignKey(
                        name: "FK_pop_ups_vouchere_id_voucher",
                        column: x => x.id_voucher,
                        principalTable: "vouchere",
                        principalColumn: "id_voucher");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_pop_ups_id_voucher",
                table: "pop_ups",
                column: "id_voucher");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pop_ups");

            migrationBuilder.UpdateData(
                table: "comenzi",
                keyColumn: "numar_factura_json",
                keyValue: null,
                column: "numar_factura_json",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "numar_factura_json",
                table: "comenzi",
                type: "json",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "json",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
