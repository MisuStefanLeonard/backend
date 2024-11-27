using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce_BackEnd.Migrations.V1_4
{
    /// <inheritdoc />
    public partial class SetUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdSet",
                table: "reviews",
                type: "int(1)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_IdSet",
                table: "reviews",
                column: "IdSet");

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_seturi_IdSet",
                table: "reviews",
                column: "IdSet",
                principalTable: "seturi",
                principalColumn: "id_set");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reviews_seturi_IdSet",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "IX_reviews_IdSet",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "IdSet",
                table: "reviews");
        }
    }
}
