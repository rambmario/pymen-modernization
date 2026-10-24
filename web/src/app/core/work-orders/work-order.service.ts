import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateWorkOrderRequest,
  PagedResult,
  WorkOrderListParams,
  WorkOrderResponse,
} from '../api/models';

@Injectable({ providedIn: 'root' })
export class WorkOrderService {
  private readonly http = inject(HttpClient);

  list(params: WorkOrderListParams = {}): Observable<PagedResult<WorkOrderResponse>> {
    let httpParams = new HttpParams()
      .set('page', String(params.page ?? 1))
      .set('pageSize', String(params.pageSize ?? 20));

    if (params.status) {
      httpParams = httpParams.set('status', params.status);
    }
    if (params.customerId) {
      httpParams = httpParams.set('customerId', params.customerId);
    }

    return this.http.get<PagedResult<WorkOrderResponse>>('/api/work-orders', { params: httpParams });
  }

  get(id: string): Observable<WorkOrderResponse> {
    return this.http.get<WorkOrderResponse>(`/api/work-orders/${id}`);
  }

  create(request: CreateWorkOrderRequest): Observable<WorkOrderResponse> {
    return this.http.post<WorkOrderResponse>('/api/work-orders', request);
  }

  start(id: string): Observable<WorkOrderResponse> {
    return this.http.post<WorkOrderResponse>(`/api/work-orders/${id}/start`, {});
  }

  deliver(id: string): Observable<WorkOrderResponse> {
    return this.http.post<WorkOrderResponse>(`/api/work-orders/${id}/deliver`, {});
  }

  invoice(id: string): Observable<WorkOrderResponse> {
    return this.http.post<WorkOrderResponse>(`/api/work-orders/${id}/invoice`, {});
  }

  cancel(id: string): Observable<WorkOrderResponse> {
    return this.http.post<WorkOrderResponse>(`/api/work-orders/${id}/cancel`, {});
  }

  startOperation(workOrderId: string, operationId: string): Observable<WorkOrderResponse> {
    return this.http.post<WorkOrderResponse>(
      `/api/work-orders/${workOrderId}/operations/${operationId}/start`,
      {},
    );
  }

  finishOperation(workOrderId: string, operationId: string): Observable<WorkOrderResponse> {
    return this.http.post<WorkOrderResponse>(
      `/api/work-orders/${workOrderId}/operations/${operationId}/finish`,
      {},
    );
  }

  cancelOperation(workOrderId: string, operationId: string): Observable<WorkOrderResponse> {
    return this.http.post<WorkOrderResponse>(
      `/api/work-orders/${workOrderId}/operations/${operationId}/cancel`,
      {},
    );
  }

  downloadDeliveryNotePdf(id: string): Observable<Blob> {
    return this.http.get(`/api/work-orders/${id}/delivery-note.pdf`, {
      responseType: 'blob',
    });
  }
}
