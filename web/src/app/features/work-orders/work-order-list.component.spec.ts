import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { WorkOrderListComponent } from './work-order-list.component';

describe('WorkOrderListComponent', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.setItem(
      'pymen.auth.session',
      JSON.stringify({
        accessToken: 'token',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        tenantId: '11111111-1111-1111-1111-111111111111',
        email: 'planner.t1@demo.local',
        roles: ['Planner'],
      }),
    );

    await TestBed.configureTestingModule({
      imports: [WorkOrderListComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideAnimationsAsync(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('renders work orders from the API', async () => {
    const fixture = TestBed.createComponent(WorkOrderListComponent);
    fixture.detectChanges();

    const listReq = http.expectOne('/api/work-orders');
    listReq.flush([
      {
        id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        number: 'OT-100',
        customerId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        status: 'Draft',
        notes: null,
        createdAt: '2026-10-19T12:00:00Z',
        startedAt: null,
        deliveredAt: null,
        invoicedAt: null,
        cancelledAt: null,
        lines: [],
        operations: [],
      },
    ]);

    const customersReq = http.expectOne('/api/customers');
    customersReq.flush([{ id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', name: 'Cliente Demo', taxId: null, isActive: true }]);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('OT-100');
    expect(text).toContain('Borrador');
    expect(text).toContain('Cliente Demo');
  });
});
