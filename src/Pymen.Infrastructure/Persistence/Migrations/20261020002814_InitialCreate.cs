using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pymen.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BillOfMaterialsLines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FinishedProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RawMaterialProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                QuantityPerUnit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BillOfMaterialsLines", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                TaxId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Customers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DeliveryNotes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Number = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                DeliveredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DeliveryNotes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "OperationTypes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OperationTypes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ProductionReceipts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                IsApplied = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductionReceipts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Products",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Sku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                IsRawMaterial = table.Column<bool>(type: "bit", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "StockBalances",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockBalances", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Tenants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                PointOfSaleCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                NextWorkOrderSequence = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tenants", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Warehouses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Warehouses", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "WorkOrders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                DeliveredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                InvoicedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkOrders", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DeliveryNoteLines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                LineNumber = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DeliveryNoteLines", x => x.Id);
                table.ForeignKey(
                    name: "FK_DeliveryNoteLines_DeliveryNotes_DeliveryNoteId",
                    column: x => x.DeliveryNoteId,
                    principalTable: "DeliveryNotes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "WorkOrderLines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                LineNumber = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkOrderLines", x => x.Id);
                table.ForeignKey(
                    name: "FK_WorkOrderLines_WorkOrders_WorkOrderId",
                    column: x => x.WorkOrderId,
                    principalTable: "WorkOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "WorkOrderOperations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OperationTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                FinishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkOrderOperations", x => x.Id);
                table.ForeignKey(
                    name: "FK_WorkOrderOperations_WorkOrders_WorkOrderId",
                    column: x => x.WorkOrderId,
                    principalTable: "WorkOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BillOfMaterialsLines_TenantId_FinishedProductId_RawMaterialProductId",
            table: "BillOfMaterialsLines",
            columns: new[] { "TenantId", "FinishedProductId", "RawMaterialProductId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Customers_TenantId_Name",
            table: "Customers",
            columns: new[] { "TenantId", "Name" });

        migrationBuilder.CreateIndex(
            name: "IX_DeliveryNoteLines_DeliveryNoteId",
            table: "DeliveryNoteLines",
            column: "DeliveryNoteId");

        migrationBuilder.CreateIndex(
            name: "IX_OperationTypes_TenantId_Code",
            table: "OperationTypes",
            columns: new[] { "TenantId", "Code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Products_TenantId_Sku",
            table: "Products",
            columns: new[] { "TenantId", "Sku" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_StockBalances_TenantId_ProductId_WarehouseId",
            table: "StockBalances",
            columns: new[] { "TenantId", "ProductId", "WarehouseId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_Name",
            table: "Tenants",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Warehouses_TenantId_Code",
            table: "Warehouses",
            columns: new[] { "TenantId", "Code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_WorkOrderLines_WorkOrderId",
            table: "WorkOrderLines",
            column: "WorkOrderId");

        migrationBuilder.CreateIndex(
            name: "IX_WorkOrderOperations_WorkOrderId",
            table: "WorkOrderOperations",
            column: "WorkOrderId");

        migrationBuilder.CreateIndex(
            name: "IX_WorkOrders_TenantId_Number",
            table: "WorkOrders",
            columns: new[] { "TenantId", "Number" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BillOfMaterialsLines");

        migrationBuilder.DropTable(
            name: "Customers");

        migrationBuilder.DropTable(
            name: "DeliveryNoteLines");

        migrationBuilder.DropTable(
            name: "OperationTypes");

        migrationBuilder.DropTable(
            name: "ProductionReceipts");

        migrationBuilder.DropTable(
            name: "Products");

        migrationBuilder.DropTable(
            name: "StockBalances");

        migrationBuilder.DropTable(
            name: "Tenants");

        migrationBuilder.DropTable(
            name: "Warehouses");

        migrationBuilder.DropTable(
            name: "WorkOrderLines");

        migrationBuilder.DropTable(
            name: "WorkOrderOperations");

        migrationBuilder.DropTable(
            name: "DeliveryNotes");

        migrationBuilder.DropTable(
            name: "WorkOrders");
    }
}
