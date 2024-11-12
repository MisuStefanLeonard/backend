using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce_BackEnd.Migrations.V1_2
{
    /// <inheritdoc />
    public partial class ReviewUpdate_NewCurtainAttribute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<sbyte>(
                name: "prindere_inele",
                table: "tipuri_galerie",
                type: "tinyint",
                nullable: false,
                defaultValue: (sbyte)0);

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id_review = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_produs = table.Column<int>(type: "integer", nullable: false),
                    id_cont = table.Column<int>(type: "integer", nullable: false),
                    numar_stele = table.Column<int>(type: "integer", nullable: false),
                    text_recenzie = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.id_review);
                    table.ForeignKey(
                        name: "FK_reviews_conturi_id_cont",
                        column: x => x.id_cont,
                        principalTable: "conturi",
                        principalColumn: "id_cont",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reviews_produse_id_produs",
                        column: x => x.id_produs,
                        principalTable: "produse",
                        principalColumn: "id_produs",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_id_cont",
                table: "reviews",
                column: "id_cont");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_id_produs",
                table: "reviews",
                column: "id_produs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropColumn(
                name: "prindere_inele",
                table: "tipuri_galerie");
        }
    }
}
