using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce_BackEnd.Migrations.V1_9
{
    /// <inheritdoc />
    public partial class CartUserUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cos_cumparaturi_conturi_id_cont",
                table: "cos_cumparaturi");

            migrationBuilder.AlterColumn<int>(
                name: "id_cont",
                table: "cos_cumparaturi",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "cos_cumparaturi",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddForeignKey(
                name: "FK_cos_cumparaturi_conturi_id_cont",
                table: "cos_cumparaturi",
                column: "id_cont",
                principalTable: "conturi",
                principalColumn: "id_cont");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cos_cumparaturi_conturi_id_cont",
                table: "cos_cumparaturi");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "cos_cumparaturi");

            migrationBuilder.AlterColumn<int>(
                name: "id_cont",
                table: "cos_cumparaturi",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_cos_cumparaturi_conturi_id_cont",
                table: "cos_cumparaturi",
                column: "id_cont",
                principalTable: "conturi",
                principalColumn: "id_cont",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
