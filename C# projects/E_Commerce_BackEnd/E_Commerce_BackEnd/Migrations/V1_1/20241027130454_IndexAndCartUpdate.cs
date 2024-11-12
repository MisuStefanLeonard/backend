using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce_BackEnd.Migrations.V1_1
{
    /// <inheritdoc />
    public partial class IndexAndCartUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "greutate",
                table: "produse");

            migrationBuilder.AddColumn<sbyte>(
                name: "afiseaza_in_noutati",
                table: "produse",
                type: "tinyint",
                nullable: false,
                defaultValue: (sbyte)0);

            migrationBuilder.AddColumn<sbyte>(
                name: "produs_limitat",
                table: "produse",
                type: "tinyint",
                nullable: false,
                defaultValue: (sbyte)0);

            migrationBuilder.CreateTable(
                name: "cos_cumparaturi",
                columns: table => new
                {
                    id_produs_in_cos = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    cantitate_produs = table.Column<int>(type: "int", nullable: false),
                    pret_produs = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    id_cont = table.Column<int>(type: "int", nullable: false),
                    id_produs = table.Column<int>(type: "int", nullable: false),
                    id_culoare = table.Column<int>(type: "int", nullable: false),
                    id_dimensiune = table.Column<int>(type: "int", nullable: false),
                    id_set = table.Column<int>(type: "int", nullable: true),
                    id_manopera = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cos_cumparaturi", x => x.id_produs_in_cos);
                    table.ForeignKey(
                        name: "FK_cos_cumparaturi_conturi_id_cont",
                        column: x => x.id_cont,
                        principalTable: "conturi",
                        principalColumn: "id_cont",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cos_cumparaturi_culori_id_culoare",
                        column: x => x.id_culoare,
                        principalTable: "culori",
                        principalColumn: "id_culoare",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cos_cumparaturi_dimensiuni_id_dimensiune",
                        column: x => x.id_dimensiune,
                        principalTable: "dimensiuni",
                        principalColumn: "id_dimensiune",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cos_cumparaturi_manopere_id_manopera",
                        column: x => x.id_manopera,
                        principalTable: "manopere",
                        principalColumn: "id_manopera");
                    table.ForeignKey(
                        name: "FK_cos_cumparaturi_produse_id_produs",
                        column: x => x.id_produs,
                        principalTable: "produse",
                        principalColumn: "id_produs",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cos_cumparaturi_seturi_id_set",
                        column: x => x.id_set,
                        principalTable: "seturi",
                        principalColumn: "id_set");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "INDEX_EMAIL",
                table: "conturi",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "INDEX_USERNAME",
                table: "conturi",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cos_cumparaturi_id_cont",
                table: "cos_cumparaturi",
                column: "id_cont");

            migrationBuilder.CreateIndex(
                name: "IX_cos_cumparaturi_id_culoare",
                table: "cos_cumparaturi",
                column: "id_culoare");

            migrationBuilder.CreateIndex(
                name: "IX_cos_cumparaturi_id_dimensiune",
                table: "cos_cumparaturi",
                column: "id_dimensiune");

            migrationBuilder.CreateIndex(
                name: "IX_cos_cumparaturi_id_manopera",
                table: "cos_cumparaturi",
                column: "id_manopera");

            migrationBuilder.CreateIndex(
                name: "IX_cos_cumparaturi_id_produs",
                table: "cos_cumparaturi",
                column: "id_produs");

            migrationBuilder.CreateIndex(
                name: "IX_cos_cumparaturi_id_set",
                table: "cos_cumparaturi",
                column: "id_set");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cos_cumparaturi");

            migrationBuilder.DropIndex(
                name: "INDEX_EMAIL",
                table: "conturi");

            migrationBuilder.DropIndex(
                name: "INDEX_USERNAME",
                table: "conturi");

            migrationBuilder.DropColumn(
                name: "afiseaza_in_noutati",
                table: "produse");

            migrationBuilder.DropColumn(
                name: "produs_limitat",
                table: "produse");

            migrationBuilder.AddColumn<decimal>(
                name: "greutate",
                table: "produse",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
