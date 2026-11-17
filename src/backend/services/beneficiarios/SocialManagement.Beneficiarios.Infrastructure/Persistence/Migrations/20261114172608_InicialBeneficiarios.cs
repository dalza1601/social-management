using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialManagement.Beneficiarios.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InicialBeneficiarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "beneficiarios");

            migrationBuilder.CreateTable(
                name: "Beneficiarios",
                schema: "beneficiarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentoTipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DocumentoNumero = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    FechaRegistroUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beneficiarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Beneficiarios_Documento",
                schema: "beneficiarios",
                table: "Beneficiarios",
                columns: new[] { "DocumentoTipo", "DocumentoNumero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Beneficiarios",
                schema: "beneficiarios");
        }
    }
}
