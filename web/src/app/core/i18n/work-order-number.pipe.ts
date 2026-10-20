import { Pipe, PipeTransform } from '@angular/core';

/**
 * Display work-order numbers as POS-sequence (0001-00000002).
 * Storage/API keep the compact 12-digit form (000100000002).
 */
@Pipe({
  name: 'workOrderNumber',
  standalone: true,
})
export class WorkOrderNumberPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) {
      return '';
    }

    if (value.includes('-')) {
      return value;
    }

    if (/^\d{12}$/.test(value)) {
      return `${value.slice(0, 4)}-${value.slice(4)}`;
    }

    return value;
  }
}
