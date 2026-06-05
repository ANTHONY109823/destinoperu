using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DestinoPeruAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class V6PartnerSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Partners",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partners_Slug",
                table: "Partners",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Partners_Slug",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Partners");
        }
    }
}
