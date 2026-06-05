using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DestinoPeruAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class V4MaintenanceAndTourRichContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DuracionAproximada",
                table: "Tours",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GaleriaJson",
                table: "Tours",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HoraSalida",
                table: "Tours",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItinerarioJson",
                table: "Tours",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PuntoPartida",
                table: "Tours",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PuntoRetorno",
                table: "Tours",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QueIncluyeJson",
                table: "Tours",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QueLlevarJson",
                table: "Tours",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QueNoIncluyeJson",
                table: "Tours",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppMaintenanceRuns",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMaintenanceRuns", x => x.Key);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppMaintenanceRuns");

            migrationBuilder.DropColumn(
                name: "DuracionAproximada",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "GaleriaJson",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "HoraSalida",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "ItinerarioJson",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "PuntoPartida",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "PuntoRetorno",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "QueIncluyeJson",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "QueLlevarJson",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "QueNoIncluyeJson",
                table: "Tours");
        }
    }
}
