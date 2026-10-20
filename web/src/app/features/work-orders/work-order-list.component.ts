import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';

import { ApiErrorService } from '../../core/api/api-error.service';
import { WORK_ORDER_STATUSES, workOrderStatusLabel } from '../../core/api/labels';
import { CustomerResponse, WorkOrderResponse } from '../../core/api/models';
import { AuthService } from '../../core/auth/auth.service';
import { CatalogService } from '../../core/catalog/catalog.service';
import { WorkOrderService } from '../../core/work-orders/work-order.service';

@Component({
  selector: 'app-work-order-list',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './work-order-list.component.html',
  styleUrl: './work-order-list.component.scss',
})
export class WorkOrderListComponent implements OnInit {
  private readonly workOrdersApi = inject(WorkOrderService);
  private readonly catalog = inject(CatalogService);
  private readonly apiErrors = inject(ApiErrorService);
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService);

  readonly loading = signal(true);
  readonly allItems = signal<WorkOrderResponse[]>([]);
  readonly customers = signal<CustomerResponse[]>([]);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);

  readonly filters = this.fb.nonNullable.group({
    status: [''],
    customerId: [''],
  });

  readonly statuses = WORK_ORDER_STATUSES;
  readonly columns = ['number', 'customer', 'status', 'createdAt', 'actions'];
  readonly statusLabel = workOrderStatusLabel;

  ngOnInit(): void {
    this.filters.valueChanges.subscribe(() => this.pageIndex.set(0));

    this.workOrdersApi.list().subscribe({
      next: (workOrders) => {
        this.allItems.set(workOrders);
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.apiErrors.showHttpError(error);
      },
    });

    if (this.auth.hasAnyRole('Admin', 'Planner')) {
      this.catalog.listCustomers().subscribe({
        next: (customers) => this.customers.set(customers),
        error: (error) => this.apiErrors.showHttpError(error),
      });
    }
  }

  filteredItems(): WorkOrderResponse[] {
    const { status, customerId } = this.filters.getRawValue();
    return this.allItems().filter((item) => {
      if (status && item.status !== status) {
        return false;
      }
      if (customerId && item.customerId !== customerId) {
        return false;
      }
      return true;
    });
  }

  pagedItems(): WorkOrderResponse[] {
    const start = this.pageIndex() * this.pageSize();
    return this.filteredItems().slice(start, start + this.pageSize());
  }

  customerName(customerId: string): string {
    return this.customers().find((c) => c.id === customerId)?.name ?? customerId.slice(0, 8);
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
  }
}
