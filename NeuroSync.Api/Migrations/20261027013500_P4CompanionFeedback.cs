using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NeuroSync.Api.Data;

#nullable disable

namespace NeuroSync.Api.Migrations;

[DbContext(typeof(NeuroSyncDbContext))]
[Migration("20261027013500_P4CompanionFeedback")]
public partial class P4CompanionFeedback : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CompanionFeedback",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                ResponseId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Rating = table.Column<int>(type: "integer", nullable: false),
                ReasonCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                ProviderId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Intent = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                QualityScore = table.Column<double>(type: "double precision", nullable: false),
                ShareForLearning = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CompanionFeedback", x => x.Id);
                table.ForeignKey(
                    name: "FK_CompanionFeedback_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CompanionFeedback_CreatedAtUtc",
            table: "CompanionFeedback",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_CompanionFeedback_ShareForLearning",
            table: "CompanionFeedback",
            column: "ShareForLearning");

        migrationBuilder.CreateIndex(
            name: "IX_CompanionFeedback_UserId_ResponseId",
            table: "CompanionFeedback",
            columns: new[] { "UserId", "ResponseId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CompanionFeedback");
    }
}
