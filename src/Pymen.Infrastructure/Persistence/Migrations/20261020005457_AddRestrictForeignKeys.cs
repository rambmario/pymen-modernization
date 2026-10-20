using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pymen.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRestrictForeignKeys : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_WorkOrders_CustomerId",
            table: "WorkOrders",
            column: "CustomerId");

        migrationBuilder.CreateIndex(
            name: "IX_WorkOrderOperations_OperationTypeId",
            table: "WorkOrderOperations",
            column: "OperationTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_WorkOrderOperations_TenantId",
            table: "WorkOrderOperations",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_WorkOrderLines_ProductId",
            table: "WorkOrderLines",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_WorkOrderLines_TenantId",
            table: "WorkOrderLines",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_StockBalances_ProductId",
            table: "StockBalances",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_StockBalances_WarehouseId",
            table: "StockBalances",
            column: "WarehouseId");

        migrationBuilder.CreateIndex(
            name: "IX_ProductionReceipts_ProductId",
            table: "ProductionReceipts",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_ProductionReceipts_TenantId",
            table: "ProductionReceipts",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_ProductionReceipts_WarehouseId",
            table: "ProductionReceipts",
            column: "WarehouseId");

        migrationBuilder.CreateIndex(
            name: "IX_ProductionReceipts_WorkOrderId",
            table: "ProductionReceipts",
            column: "WorkOrderId");

        migrationBuilder.CreateIndex(
            name: "IX_DeliveryNotes_TenantId",
            table: "DeliveryNotes",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_DeliveryNotes_WorkOrderId",
            table: "DeliveryNotes",
            column: "WorkOrderId");

        migrationBuilder.CreateIndex(
            name: "IX_DeliveryNoteLines_ProductId",
            table: "DeliveryNoteLines",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_DeliveryNoteLines_TenantId",
            table: "DeliveryNoteLines",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_BillOfMaterialsLines_FinishedProductId",
            table: "BillOfMaterialsLines",
            column: "FinishedProductId");

        migrationBuilder.CreateIndex(
            name: "IX_BillOfMaterialsLines_RawMaterialProductId",
            table: "BillOfMaterialsLines",
            column: "RawMaterialProductId");

        migrationBuilder.AddForeignKey(
            name: "FK_BillOfMaterialsLines_Products_FinishedProductId",
            table: "BillOfMaterialsLines",
            column: "FinishedProductId",
            principalTable: "Products",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_BillOfMaterialsLines_Products_RawMaterialProductId",
            table: "BillOfMaterialsLines",
            column: "RawMaterialProductId",
            principalTable: "Products",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_BillOfMaterialsLines_Tenants_TenantId",
            table: "BillOfMaterialsLines",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Customers_Tenants_TenantId",
            table: "Customers",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_DeliveryNoteLines_Products_ProductId",
            table: "DeliveryNoteLines",
            column: "ProductId",
            principalTable: "Products",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_DeliveryNoteLines_Tenants_TenantId",
            table: "DeliveryNoteLines",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_DeliveryNotes_Tenants_TenantId",
            table: "DeliveryNotes",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_DeliveryNotes_WorkOrders_WorkOrderId",
            table: "DeliveryNotes",
            column: "WorkOrderId",
            principalTable: "WorkOrders",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_OperationTypes_Tenants_TenantId",
            table: "OperationTypes",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ProductionReceipts_Products_ProductId",
            table: "ProductionReceipts",
            column: "ProductId",
            principalTable: "Products",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ProductionReceipts_Tenants_TenantId",
            table: "ProductionReceipts",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ProductionReceipts_Warehouses_WarehouseId",
            table: "ProductionReceipts",
            column: "WarehouseId",
            principalTable: "Warehouses",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ProductionReceipts_WorkOrders_WorkOrderId",
            table: "ProductionReceipts",
            column: "WorkOrderId",
            principalTable: "WorkOrders",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Products_Tenants_TenantId",
            table: "Products",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_StockBalances_Products_ProductId",
            table: "StockBalances",
            column: "ProductId",
            principalTable: "Products",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_StockBalances_Tenants_TenantId",
            table: "StockBalances",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_StockBalances_Warehouses_WarehouseId",
            table: "StockBalances",
            column: "WarehouseId",
            principalTable: "Warehouses",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Warehouses_Tenants_TenantId",
            table: "Warehouses",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_WorkOrderLines_Products_ProductId",
            table: "WorkOrderLines",
            column: "ProductId",
            principalTable: "Products",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_WorkOrderLines_Tenants_TenantId",
            table: "WorkOrderLines",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_WorkOrderOperations_OperationTypes_OperationTypeId",
            table: "WorkOrderOperations",
            column: "OperationTypeId",
            principalTable: "OperationTypes",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_WorkOrderOperations_Tenants_TenantId",
            table: "WorkOrderOperations",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_WorkOrders_Customers_CustomerId",
            table: "WorkOrders",
            column: "CustomerId",
            principalTable: "Customers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_WorkOrders_Tenants_TenantId",
            table: "WorkOrders",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_BillOfMaterialsLines_Products_FinishedProductId",
            table: "BillOfMaterialsLines");

        migrationBuilder.DropForeignKey(
            name: "FK_BillOfMaterialsLines_Products_RawMaterialProductId",
            table: "BillOfMaterialsLines");

        migrationBuilder.DropForeignKey(
            name: "FK_BillOfMaterialsLines_Tenants_TenantId",
            table: "BillOfMaterialsLines");

        migrationBuilder.DropForeignKey(
            name: "FK_Customers_Tenants_TenantId",
            table: "Customers");

        migrationBuilder.DropForeignKey(
            name: "FK_DeliveryNoteLines_Products_ProductId",
            table: "DeliveryNoteLines");

        migrationBuilder.DropForeignKey(
            name: "FK_DeliveryNoteLines_Tenants_TenantId",
            table: "DeliveryNoteLines");

        migrationBuilder.DropForeignKey(
            name: "FK_DeliveryNotes_Tenants_TenantId",
            table: "DeliveryNotes");

        migrationBuilder.DropForeignKey(
            name: "FK_DeliveryNotes_WorkOrders_WorkOrderId",
            table: "DeliveryNotes");

        migrationBuilder.DropForeignKey(
            name: "FK_OperationTypes_Tenants_TenantId",
            table: "OperationTypes");

        migrationBuilder.DropForeignKey(
            name: "FK_ProductionReceipts_Products_ProductId",
            table: "ProductionReceipts");

        migrationBuilder.DropForeignKey(
            name: "FK_ProductionReceipts_Tenants_TenantId",
            table: "ProductionReceipts");

        migrationBuilder.DropForeignKey(
            name: "FK_ProductionReceipts_Warehouses_WarehouseId",
            table: "ProductionReceipts");

        migrationBuilder.DropForeignKey(
            name: "FK_ProductionReceipts_WorkOrders_WorkOrderId",
            table: "ProductionReceipts");

        migrationBuilder.DropForeignKey(
            name: "FK_Products_Tenants_TenantId",
            table: "Products");

        migrationBuilder.DropForeignKey(
            name: "FK_StockBalances_Products_ProductId",
            table: "StockBalances");

        migrationBuilder.DropForeignKey(
            name: "FK_StockBalances_Tenants_TenantId",
            table: "StockBalances");

        migrationBuilder.DropForeignKey(
            name: "FK_StockBalances_Warehouses_WarehouseId",
            table: "StockBalances");

        migrationBuilder.DropForeignKey(
            name: "FK_Warehouses_Tenants_TenantId",
            table: "Warehouses");

        migrationBuilder.DropForeignKey(
            name: "FK_WorkOrderLines_Products_ProductId",
            table: "WorkOrderLines");

        migrationBuilder.DropForeignKey(
            name: "FK_WorkOrderLines_Tenants_TenantId",
            table: "WorkOrderLines");

        migrationBuilder.DropForeignKey(
            name: "FK_WorkOrderOperations_OperationTypes_OperationTypeId",
            table: "WorkOrderOperations");

        migrationBuilder.DropForeignKey(
            name: "FK_WorkOrderOperations_Tenants_TenantId",
            table: "WorkOrderOperations");

        migrationBuilder.DropForeignKey(
            name: "FK_WorkOrders_Customers_CustomerId",
            table: "WorkOrders");

        migrationBuilder.DropForeignKey(
            name: "FK_WorkOrders_Tenants_TenantId",
            table: "WorkOrders");

        migrationBuilder.DropIndex(
            name: "IX_WorkOrders_CustomerId",
            table: "WorkOrders");

        migrationBuilder.DropIndex(
            name: "IX_WorkOrderOperations_OperationTypeId",
            table: "WorkOrderOperations");

        migrationBuilder.DropIndex(
            name: "IX_WorkOrderOperations_TenantId",
            table: "WorkOrderOperations");

        migrationBuilder.DropIndex(
            name: "IX_WorkOrderLines_ProductId",
            table: "WorkOrderLines");

        migrationBuilder.DropIndex(
            name: "IX_WorkOrderLines_TenantId",
            table: "WorkOrderLines");

        migrationBuilder.DropIndex(
            name: "IX_StockBalances_ProductId",
            table: "StockBalances");

        migrationBuilder.DropIndex(
            name: "IX_StockBalances_WarehouseId",
            table: "StockBalances");

        migrationBuilder.DropIndex(
            name: "IX_ProductionReceipts_ProductId",
            table: "ProductionReceipts");

        migrationBuilder.DropIndex(
            name: "IX_ProductionReceipts_TenantId",
            table: "ProductionReceipts");

        migrationBuilder.DropIndex(
            name: "IX_ProductionReceipts_WarehouseId",
            table: "ProductionReceipts");

        migrationBuilder.DropIndex(
            name: "IX_ProductionReceipts_WorkOrderId",
            table: "ProductionReceipts");

        migrationBuilder.DropIndex(
            name: "IX_DeliveryNotes_TenantId",
            table: "DeliveryNotes");

        migrationBuilder.DropIndex(
            name: "IX_DeliveryNotes_WorkOrderId",
            table: "DeliveryNotes");

        migrationBuilder.DropIndex(
            name: "IX_DeliveryNoteLines_ProductId",
            table: "DeliveryNoteLines");

        migrationBuilder.DropIndex(
            name: "IX_DeliveryNoteLines_TenantId",
            table: "DeliveryNoteLines");

        migrationBuilder.DropIndex(
            name: "IX_BillOfMaterialsLines_FinishedProductId",
            table: "BillOfMaterialsLines");

        migrationBuilder.DropIndex(
            name: "IX_BillOfMaterialsLines_RawMaterialProductId",
            table: "BillOfMaterialsLines");
    }
}

