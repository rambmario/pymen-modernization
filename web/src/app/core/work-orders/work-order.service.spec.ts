import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { WorkOrderService } from './work-order.service';

describe('WorkOrderService', () => {
  let service: WorkOrderService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(WorkOrderService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists work orders', () => {
    service.list().subscribe((items) => {
      expect(items.length).toBe(1);
      expect(items[0].number).toBe('OT-1');
    });

    const req = http.expectOne('/api/work-orders');
    expect(req.request.method).toBe('GET');
    req.flush([
      {
        id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        number: 'OT-1',
        customerId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        status: 'Draft',
        notes: null,
        createdAt: new Date().toISOString(),
        startedAt: null,
        deliveredAt: null,
        invoicedAt: null,
        cancelledAt: null,
        lines: [],
        operations: [],
      },
    ]);
  });

  it('posts start action', () => {
    const id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
    service.start(id).subscribe();
    const req = http.expectOne(`/api/work-orders/${id}/start`);
    expect(req.request.method).toBe('POST');
    req.flush({ id, number: 'OT-1', status: 'InProduction' });
  });
});
