import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ApiErrorService } from '../../core/api/api-error.service';
import { WORK_ORDER_STATUSES, workOrderStatusLabel } from '../../core/api/labels';
import { CustomerResponse, WorkOrderResponse } from '../../core/api/models';
import { AuthService } from '../../core/auth/auth.service';
import { CatalogService } from '../../core/catalog/catalog.service';
import { PYMEN_DATE_TIME } from '../../core/i18n/date-formats';
import { WorkOrderNumberPipe } from '../../core/i18n/work-order-number.pipe';
import { WorkOrderService } from '../../core/work-orders/work-order.service';

@Component({
  selector: 'app-work-order-list',
  imports: [
    DatePipe,
    WorkOrderNumberPipe,
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
  private readonly destroyRef = inject(DestroyRef);
  readonly auth = inject(AuthService);

  readonly loading = signal(true);
  readonly items = signal<WorkOrderResponse[]>([]);
  readonly totalCount = signal(0);
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
  readonly dateTime = PYMEN_DATE_TIME;

  ngOnInit(): void {
    this.filters.valueChanges
      .pipe(debounceTime(0), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.loadPage();
      });

    if (this.auth.hasAnyRole('Admin', 'Planner')) {
      this.catalog
        .listCustomers()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (customers) => this.customers.set(customers),
          error: (error) => this.apiErrors.showHttpError(error),
        });
    }

    this.loadPage();
  }

  customerName(customerId: string): string {
    return this.customers().find((c) => c.id === customerId)?.name ?? customerId.slice(0, 8);
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadPage();
  }

  private loadPage(): void {
    this.loading.set(true);
    const { status, customerId } = this.filters.getRawValue();

    this.workOrdersApi
      .list({
        status: status || undefined,
        customerId: customerId || undefined,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: (error) => {
          this.loading.set(false);
          this.apiErrors.showHttpError(error);
        },
      });
  }
}
