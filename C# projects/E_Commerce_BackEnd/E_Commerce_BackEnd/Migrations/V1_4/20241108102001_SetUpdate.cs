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
                name: "id_set",
                table: "reviews",
                type: "int(1)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_id_set",
                table: "reviews",
                column: "id_set");

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_seturi_id_set",
                table: "reviews",
                column: "IdSet",
                principalTable: "seturi",
                principalColumn: "id_set");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reviews_seturi_id_set",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "IX_reviews_id_set",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "id_set",
                table: "reviews");
        }
    }
}
