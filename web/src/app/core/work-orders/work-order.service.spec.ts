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

  it('lists work orders with paging query params', () => {
    service.list({ status: 'Draft', page: 2, pageSize: 10 }).subscribe((result) => {
      expect(result.items.length).toBe(1);
      expect(result.totalCount).toBe(3);
      expect(result.page).toBe(2);
    });

    const req = http.expectOne((r) => r.url === '/api/work-orders');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('status')).toBe('Draft');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    req.flush({
      items: [
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
      ],
      page: 2,
      pageSize: 10,
      totalCount: 3,
    });
  });

  it('posts start action', () => {
    const id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
    service.start(id).subscribe();
    const req = http.expectOne(`/api/work-orders/${id}/start`);
    expect(req.request.method).toBe('POST');
    req.flush({ id, number: 'OT-1', status: 'InProduction' });
  });
});
