import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CustomerResponse, OperationTypeResponse, ProductResponse } from '../api/models';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);

  listCustomers(): Observable<CustomerResponse[]> {
    return this.http.get<CustomerResponse[]>('/api/customers');
  }

  listProducts(): Observable<ProductResponse[]> {
    return this.http.get<ProductResponse[]>('/api/products');
  }

  listOperationTypes(): Observable<OperationTypeResponse[]> {
    return this.http.get<OperationTypeResponse[]>('/api/operation-types');
  }
}
