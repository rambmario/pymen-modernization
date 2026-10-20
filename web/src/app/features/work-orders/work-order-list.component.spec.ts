import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { APP_LOCALE, ensureAppLocale } from '../../core/i18n/locale';
import { SpanishPaginatorIntl } from '../../core/i18n/spanish-paginator-intl';
import { WorkOrderListComponent } from './work-order-list.component';

describe('WorkOrderListComponent', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    ensureAppLocale();
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
        { provide: LOCALE_ID, useValue: APP_LOCALE },
        { provide: MatPaginatorIntl, useClass: SpanishPaginatorIntl },
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('renders work orders from the paged API', async () => {
    const fixture = TestBed.createComponent(WorkOrderListComponent);
    fixture.detectChanges();

    const listReq = http.expectOne((r) => r.url === '/api/work-orders');
    expect(listReq.request.params.get('page')).toBe('1');
    listReq.flush({
      items: [
        {
          id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          number: '000100000100',
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
      ],
      page: 1,
      pageSize: 10,
      totalCount: 1,
    });

    const customersReq = http.expectOne('/api/customers');
    customersReq.flush([
      { id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', name: 'Cliente Demo', taxId: null, isActive: true },
    ]);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('0001-00000100');
    expect(text).toContain('Borrador');
    expect(text).toContain('Cliente Demo');
    expect(text).toContain('Ítems por página');
    expect(text).toMatch(/1\s*[–-]\s*1 de 1/);
  });
});
