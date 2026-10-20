const workOrderStatusLabels: Record<string, string> = {
  Draft: 'Borrador',
  InProduction: 'En producción',
  Delivered: 'Entregada',
  Invoiced: 'Facturada',
  Cancelled: 'Cancelada',
};

const operationStatusLabels: Record<string, string> = {
  Pending: 'Pendiente',
  Started: 'Iniciada',
  Finished: 'Finalizada',
  Cancelled: 'Cancelada',
};

export function workOrderStatusLabel(status: string): string {
  return workOrderStatusLabels[status] ?? status;
}

export function operationStatusLabel(status: string): string {
  return operationStatusLabels[status] ?? status;
}

export const WORK_ORDER_STATUSES = Object.keys(workOrderStatusLabels);
