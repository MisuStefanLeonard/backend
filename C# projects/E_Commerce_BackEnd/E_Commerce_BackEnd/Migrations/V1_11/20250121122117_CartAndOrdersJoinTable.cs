using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce_BackEnd.Migrations.V1_11
{
    /// <inheritdoc />
    public partial class CartAndOrdersJoinTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "inaltime_set",
                table: "produse_cu_comenzi",
                type: "varchar(4)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "expires_at",
                table: "cos_cumparaturi",
                type: "datetime",
                nullable: false,
                defaultValueSql: "NOW()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "inaltime_set",
                table: "produse_cu_comenzi");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "cos_cumparaturi");
        }
    }
}
