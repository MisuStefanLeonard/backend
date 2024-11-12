using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce_BackEnd.Migrations.V1_3
{
    /// <inheritdoc />
    public partial class CurtainAndRingsUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pret_curent_inele_prindere",
                table: "manopere");

            migrationBuilder.DropColumn(
                name: "pret_metru_inele",
                table: "inele_prindere");

            migrationBuilder.AddColumn<decimal>(
                name: "incretire",
                table: "tipuri_galerie",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "material_folosit",
                table: "manopere",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "incretire",
                table: "tipuri_galerie");

            migrationBuilder.DropColumn(
                name: "material_folosit",
                table: "manopere");

            migrationBuilder.AddColumn<decimal>(
                name: "pret_curent_inele_prindere",
                table: "manopere",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "pret_metru_inele",
                table: "inele_prindere",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
