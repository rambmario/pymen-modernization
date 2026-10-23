using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pymen.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddOutbox : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Type = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                AttemptCount = table.Column<int>(type: "int", nullable: false),
                LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OutboxMessages", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_Status_OccurredAt",
            table: "OutboxMessages",
            columns: new[] { "Status", "OccurredAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "OutboxMessages");
    }
}
