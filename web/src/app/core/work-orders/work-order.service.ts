import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateWorkOrderRequest, WorkOrderResponse } from '../api/models';

@Injectable({ providedIn: 'root' })
export class WorkOrderService {
  private readonly http = inject(HttpClient);

  list(): Observable<WorkOrderResponse[]> {
    return this.http.get<WorkOrderResponse[]>('/api/work-orders');
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
}
