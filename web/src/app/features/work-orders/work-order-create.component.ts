import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { forkJoin } from 'rxjs';
import { ApiErrorService } from '../../core/api/api-error.service';
import { CustomerResponse, OperationTypeResponse, ProductResponse } from '../../core/api/models';
import { CatalogService } from '../../core/catalog/catalog.service';
import { WorkOrderService } from '../../core/work-orders/work-order.service';

@Component({
  selector: 'app-work-order-create',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './work-order-create.component.html',
  styleUrl: './work-order-create.component.scss',
})
export class WorkOrderCreateComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly catalog = inject(CatalogService);
  private readonly workOrdersApi = inject(WorkOrderService);
  private readonly apiErrors = inject(ApiErrorService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly customers = signal<CustomerResponse[]>([]);
  readonly products = signal<ProductResponse[]>([]);
  readonly operationTypes = signal<OperationTypeResponse[]>([]);

  readonly form = this.fb.nonNullable.group({
    customerId: ['', Validators.required],
    notes: [''],
    lines: this.fb.array([this.createLineGroup()]),
    operations: this.fb.array([this.createOperationGroup()]),
  });

  get lines(): FormArray {
    return this.form.controls.lines;
  }

  get operations(): FormArray {
    return this.form.controls.operations;
  }

  ngOnInit(): void {
    forkJoin({
      customers: this.catalog.listCustomers(),
      products: this.catalog.listProducts(),
      operationTypes: this.catalog.listOperationTypes(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ customers, products, operationTypes }) => {
          this.customers.set(customers.filter((c) => c.isActive));
          this.products.set(products.filter((p) => p.isActive && !p.isRawMaterial));
          this.operationTypes.set(operationTypes);
          this.loading.set(false);
        },
        error: (error) => {
          this.loading.set(false);
          this.apiErrors.showHttpError(error);
        },
      });
  }

  addLine(): void {
    this.lines.push(this.createLineGroup());
  }

  removeLine(index: number): void {
    if (this.lines.length > 1) {
      this.lines.removeAt(index);
    }
  }

  addOperation(): void {
    this.operations.push(this.createOperationGroup());
  }

  removeOperation(index: number): void {
    this.operations.removeAt(index);
  }

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.saving.set(true);
    this.workOrdersApi
      .create({
        customerId: raw.customerId,
        notes: raw.notes.trim() || null,
        lines: raw.lines.map((line) => ({
          productId: line.productId,
          quantity: Number(line.quantity),
        })),
        operations: raw.operations
          .filter((op) => !!op.operationTypeId)
          .map((op) => ({ operationTypeId: op.operationTypeId })),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => {
          this.saving.set(false);
          void this.router.navigate(['/work-orders', created.id]);
        },
        error: (error) => {
          this.saving.set(false);
          this.apiErrors.showHttpError(error);
        },
      });
  }

  private createLineGroup() {
    return this.fb.nonNullable.group({
      productId: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(0.0001)]],
    });
  }

  private createOperationGroup() {
    return this.fb.nonNullable.group({
      operationTypeId: [''],
    });
  }
}
