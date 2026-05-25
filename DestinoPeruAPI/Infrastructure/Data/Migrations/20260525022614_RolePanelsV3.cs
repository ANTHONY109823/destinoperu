using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DestinoPeruAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RolePanelsV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Partners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Partners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Partners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingDepartment",
                table: "Partners",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PartnerStaff",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PartnerId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    StaffRole = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerStaff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerStaff_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartnerStaff_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerStaff_PartnerId_UserId",
                table: "PartnerStaff",
                columns: new[] { "PartnerId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerStaff_UserId",
                table: "PartnerStaff",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartnerStaff");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "OperatingDepartment",
                table: "Partners");
        }
    }
}
