using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce_BackEnd.Migrations.V1_5
{
    /// <inheritdoc />
    public partial class AsocSeturiAndManoperaUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tip_manopera",
                table: "manopere",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "id_manopera",
                table: "asociere_seturi",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_asociere_seturi_id_manopera",
                table: "asociere_seturi",
                column: "id_manopera");

            migrationBuilder.AddForeignKey(
                name: "FK_Manopera_AsociereSeturi",
                table: "asociere_seturi",
                column: "id_manopera",
                principalTable: "manopere",
                principalColumn: "id_manopera");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Manopera_AsociereSeturi",
                table: "asociere_seturi");

            migrationBuilder.DropIndex(
                name: "IX_asociere_seturi_id_manopera",
                table: "asociere_seturi");

            migrationBuilder.DropColumn(
                name: "tip_manopera",
                table: "manopere");

            migrationBuilder.DropColumn(
                name: "id_manopera",
                table: "asociere_seturi");
        }
    }
}
