using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pymen.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class OutboxLockAndRetry : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_Status_OccurredAt",
            table: "OutboxMessages");

        migrationBuilder.AddColumn<string>(
            name: "LockedBy",
            table: "OutboxMessages",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "LockedUntil",
            table: "OutboxMessages",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "NextAttemptAt",
            table: "OutboxMessages",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_LockedUntil",
            table: "OutboxMessages",
            column: "LockedUntil");

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_Status_NextAttemptAt_OccurredAt",
            table: "OutboxMessages",
            columns: new[] { "Status", "NextAttemptAt", "OccurredAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_LockedUntil",
            table: "OutboxMessages");

        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_Status_NextAttemptAt_OccurredAt",
            table: "OutboxMessages");

        migrationBuilder.DropColumn(
            name: "LockedBy",
            table: "OutboxMessages");

        migrationBuilder.DropColumn(
            name: "LockedUntil",
            table: "OutboxMessages");

        migrationBuilder.DropColumn(
            name: "NextAttemptAt",
            table: "OutboxMessages");

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_Status_OccurredAt",
            table: "OutboxMessages",
            columns: new[] { "Status", "OccurredAt" });
    }
}
