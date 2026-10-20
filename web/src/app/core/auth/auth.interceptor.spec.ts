import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let auth: AuthService;

  beforeEach(() => {
    sessionStorage.clear();
    sessionStorage.setItem(
      'pymen.auth.session',
      JSON.stringify({
        accessToken: 'bearer-token',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        tenantId: '11111111-1111-1111-1111-111111111111',
        email: 'planner.t1@demo.local',
        roles: ['Planner'],
      }),
    );

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('adds Authorization Bearer header when a token exists', () => {
    expect(auth.getAccessToken()).toBe('bearer-token');

    http.get('/api/work-orders').subscribe();
    const req = httpMock.expectOne('/api/work-orders');
    expect(req.request.headers.get('Authorization')).toBe('Bearer bearer-token');
    req.flush([]);
  });

  it('logs out on 401 for non-login requests', () => {
    const logoutSpy = vi.spyOn(auth, 'logout').mockImplementation(() => undefined);

    http.get('/api/work-orders').subscribe({ error: () => undefined });
    const req = httpMock.expectOne('/api/work-orders');
    req.flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(logoutSpy).toHaveBeenCalled();
  });
});
