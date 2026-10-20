/** Hand-written API contracts mirroring Pymen.Application DTOs (camelCase JSON). */

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  tenantId: string;
  email: string;
  roles: string[];
}

export interface CustomerResponse {
  id: string;
  name: string;
  taxId: string | null;
  isActive: boolean;
}

export interface ProductResponse {
  id: string;
  sku: string;
  name: string;
  isRawMaterial: boolean;
  isActive: boolean;
}

export interface OperationTypeResponse {
  id: string;
  code: string;
  name: string;
}

export type WorkOrderStatus =
  | 'Draft'
  | 'InProduction'
  | 'Delivered'
  | 'Invoiced'
  | 'Cancelled';

export type OperationStatus = 'Pending' | 'Started' | 'Finished' | 'Cancelled';

export interface WorkOrderLineResponse {
  id: string;
  productId: string;
  quantity: number;
  lineNumber: number;
}

export interface WorkOrderOperationResponse {
  id: string;
  operationTypeId: string;
  status: OperationStatus | string;
  startedAt: string | null;
  finishedAt: string | null;
}

export interface WorkOrderResponse {
  id: string;
  number: string;
  customerId: string;
  status: WorkOrderStatus | string;
  notes: string | null;
  createdAt: string;
  startedAt: string | null;
  deliveredAt: string | null;
  invoicedAt: string | null;
  cancelledAt: string | null;
  lines: WorkOrderLineResponse[];
  operations: WorkOrderOperationResponse[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface WorkOrderListParams {
  status?: string;
  customerId?: string;
  page?: number;
  pageSize?: number;
}

export interface WorkOrderLineRequest {
  productId: string;
  quantity: number;
}

export interface WorkOrderOperationRequest {
  operationTypeId: string;
}

export interface CreateWorkOrderRequest {
  customerId: string;
  notes: string | null;
  lines: WorkOrderLineRequest[];
  operations: WorkOrderOperationRequest[] | null;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}
