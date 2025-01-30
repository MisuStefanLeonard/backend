using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce_BackEnd.Migrations.V1_6
{
    /// <inheritdoc />
    public partial class CartUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cos_cumparaturi_dimensiuni_id_dimensiune",
                table: "cos_cumparaturi");

            migrationBuilder.AlterColumn<int>(
                name: "id_dimensiune",
                table: "cos_cumparaturi",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_cos_cumparaturi_dimensiuni_id_dimensiune",
                table: "cos_cumparaturi",
                column: "id_dimensiune",
                principalTable: "dimensiuni",
                principalColumn: "id_dimensiune");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cos_cumparaturi_dimensiuni_id_dimensiune",
                table: "cos_cumparaturi");

            migrationBuilder.AlterColumn<int>(
                name: "id_dimensiune",
                table: "cos_cumparaturi",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_cos_cumparaturi_dimensiuni_id_dimensiune",
                table: "cos_cumparaturi",
                column: "id_dimensiune",
                principalTable: "dimensiuni",
                principalColumn: "id_dimensiune",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
