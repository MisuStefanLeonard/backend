using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce_BackEnd.Migrations.V1_17
{
    /// <inheritdoc />
    public partial class AddedLockedColumnToTransactionalTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<sbyte>(
                name: "is_locked",
                table: "tipuri_linie",
                type: "tinyint",
                nullable: false,
                defaultValue: (sbyte)0);

            migrationBuilder.AddColumn<sbyte>(
                name: "is_locked",
                table: "tipuri_galerie",
                type: "tinyint",
                nullable: false,
                defaultValue: (sbyte)0);

            migrationBuilder.AddColumn<sbyte>(
                name: "is_locked",
                table: "seturi",
                type: "tinyint",
                nullable: false,
                defaultValue: (sbyte)0);

            migrationBuilder.AddColumn<sbyte>(
                name: "is_locked",
                table: "produse",
                type: "tinyint",
                nullable: false,
                defaultValue: (sbyte)0);

            migrationBuilder.AddColumn<sbyte>(
                name: "is_locked",
                table: "manopere",
                type: "tinyint",
                nullable: false,
                defaultValue: (sbyte)0);

            migrationBuilder.AddColumn<sbyte>(
                name: "is_locked",
                table: "inele_prindere",
                type: "tinyint",
                nullable: false,
                defaultValue: (sbyte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_locked",
                table: "tipuri_linie");

            migrationBuilder.DropColumn(
                name: "is_locked",
                table: "tipuri_galerie");

            migrationBuilder.DropColumn(
                name: "is_locked",
                table: "seturi");

            migrationBuilder.DropColumn(
                name: "is_locked",
                table: "produse");

            migrationBuilder.DropColumn(
                name: "is_locked",
                table: "manopere");

            migrationBuilder.DropColumn(
                name: "is_locked",
                table: "inele_prindere");
        }
    }
}
