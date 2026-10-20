import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { forkJoin, of } from 'rxjs';
import { ApiErrorService } from '../../core/api/api-error.service';
import { operationStatusLabel, workOrderStatusLabel } from '../../core/api/labels';
import {
  OperationTypeResponse,
  ProductResponse,
  WorkOrderOperationResponse,
  WorkOrderResponse,
} from '../../core/api/models';
import { AuthService } from '../../core/auth/auth.service';
import { CatalogService } from '../../core/catalog/catalog.service';
import { WorkOrderService } from '../../core/work-orders/work-order.service';

@Component({
  selector: 'app-work-order-detail',
  imports: [
    DatePipe,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatTableModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './work-order-detail.component.html',
  styleUrl: './work-order-detail.component.scss',
})
export class WorkOrderDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly workOrdersApi = inject(WorkOrderService);
  private readonly catalog = inject(CatalogService);
  private readonly apiErrors = inject(ApiErrorService);
  readonly auth = inject(AuthService);

  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly workOrder = signal<WorkOrderResponse | null>(null);
  readonly products = signal<ProductResponse[]>([]);
  readonly operationTypes = signal<OperationTypeResponse[]>([]);

  readonly lineColumns = ['lineNumber', 'product', 'quantity'];
  readonly operationColumns = ['type', 'status', 'startedAt', 'finishedAt', 'actions'];
  readonly statusLabel = workOrderStatusLabel;
  readonly opStatusLabel = operationStatusLabel;

  private workOrderId = '';

  ngOnInit(): void {
    this.workOrderId = this.route.snapshot.paramMap.get('id') ?? '';
    this.reload();
  }

  canManageWorkOrder(): boolean {
    return this.auth.hasAnyRole('Admin', 'Planner');
  }

  canManageOperations(): boolean {
    return this.auth.hasAnyRole('Admin', 'Planner', 'Operator');
  }

  productLabel(productId: string): string {
    const product = this.products().find((p) => p.id === productId);
    return product ? `${product.sku} — ${product.name}` : productId.slice(0, 8);
  }

  operationTypeLabel(operationTypeId: string): string {
    const type = this.operationTypes().find((t) => t.id === operationTypeId);
    return type ? `${type.code} — ${type.name}` : operationTypeId.slice(0, 8);
  }

  showStartWorkOrder(wo: WorkOrderResponse): boolean {
    return this.canManageWorkOrder() && wo.status === 'Draft';
  }

  showDeliver(wo: WorkOrderResponse): boolean {
    return this.canManageWorkOrder() && wo.status === 'InProduction';
  }

  showInvoice(wo: WorkOrderResponse): boolean {
    return this.canManageWorkOrder() && wo.status === 'Delivered';
  }

  showCancelWorkOrder(wo: WorkOrderResponse): boolean {
    return this.canManageWorkOrder() && (wo.status === 'Draft' || wo.status === 'InProduction');
  }

  showStartOp(op: WorkOrderOperationResponse): boolean {
    return this.canManageOperations() && op.status === 'Pending';
  }

  showFinishOp(op: WorkOrderOperationResponse): boolean {
    return this.canManageOperations() && op.status === 'Started';
  }

  showCancelOp(op: WorkOrderOperationResponse): boolean {
    return this.canManageOperations() && (op.status === 'Pending' || op.status === 'Started');
  }

  run(action: () => ReturnType<WorkOrderService['start']>): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    action().subscribe({
      next: (updated) => {
        this.workOrder.set(updated);
        this.busy.set(false);
      },
      error: (error) => {
        this.busy.set(false);
        this.apiErrors.showHttpError(error);
      },
    });
  }

  startWorkOrder(): void {
    this.run(() => this.workOrdersApi.start(this.workOrderId));
  }

  deliver(): void {
    this.run(() => this.workOrdersApi.deliver(this.workOrderId));
  }

  invoice(): void {
    this.run(() => this.workOrdersApi.invoice(this.workOrderId));
  }

  cancelWorkOrder(): void {
    this.run(() => this.workOrdersApi.cancel(this.workOrderId));
  }

  startOperation(operationId: string): void {
    this.run(() => this.workOrdersApi.startOperation(this.workOrderId, operationId));
  }

  finishOperation(operationId: string): void {
    this.run(() => this.workOrdersApi.finishOperation(this.workOrderId, operationId));
  }

  cancelOperation(operationId: string): void {
    this.run(() => this.workOrdersApi.cancelOperation(this.workOrderId, operationId));
  }

  private reload(): void {
    this.loading.set(true);
    const products$ = this.auth.hasAnyRole('Admin', 'Planner')
      ? this.catalog.listProducts()
      : of<ProductResponse[]>([]);

    forkJoin({
      workOrder: this.workOrdersApi.get(this.workOrderId),
      products: products$,
      operationTypes: this.catalog.listOperationTypes(),
    }).subscribe({
      next: ({ workOrder, products, operationTypes }) => {
        this.workOrder.set(workOrder);
        this.products.set(products);
        this.operationTypes.set(operationTypes);
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.apiErrors.showHttpError(error);
      },
    });
  }
}
