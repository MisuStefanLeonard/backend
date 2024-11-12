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
            migrationBuilder.DropForeignKey(
                name: "FK_reviews_produse_id_produs",
                table: "reviews");

            migrationBuilder.AlterColumn<int>(
                name: "id_produs",
                table: "reviews",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "id_set",
                table: "reviews",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_id_set",
                table: "reviews",
                column: "id_set");

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_produse_id_produs",
                table: "reviews",
                column: "id_produs",
                principalTable: "produse",
                principalColumn: "id_produs");

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_seturi_id_set",
                table: "reviews",
                column: "id_set",
                principalTable: "seturi",
                principalColumn: "id_set");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reviews_produse_id_produs",
                table: "reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_reviews_seturi_id_set",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "IX_reviews_id_set",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "id_set",
                table: "reviews");

            migrationBuilder.AlterColumn<int>(
                name: "id_produs",
                table: "reviews",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_produse_id_produs",
                table: "reviews",
                column: "id_produs",
                principalTable: "produse",
                principalColumn: "id_produs",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
