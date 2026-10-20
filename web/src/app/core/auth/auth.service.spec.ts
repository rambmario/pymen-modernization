import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('stores session in memory and sessionStorage on login', () => {
    const service = TestBed.inject(AuthService);
    service.login({ email: 'admin.t1@demo.local', password: 'Password1!' }).subscribe();

    const req = http.expectOne('/api/auth/login');
    req.flush({
      accessToken: 'token-abc',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      tenantId: '11111111-1111-1111-1111-111111111111',
      email: 'admin.t1@demo.local',
      roles: ['Admin'],
    });

    expect(service.isAuthenticated()).toBe(true);
    expect(service.getAccessToken()).toBe('token-abc');
    expect(service.hasAnyRole('Admin')).toBe(true);
    expect(sessionStorage.getItem('pymen.auth.session')).toContain('token-abc');
  });

  it('restores session from sessionStorage and clears it on logout', () => {
    sessionStorage.setItem(
      'pymen.auth.session',
      JSON.stringify({
        accessToken: 'token-abc',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        tenantId: '11111111-1111-1111-1111-111111111111',
        email: 'admin.t1@demo.local',
        roles: ['Admin'],
      }),
    );

    const service = TestBed.inject(AuthService);
    expect(service.isAuthenticated()).toBe(true);
    service.logout(false);
    expect(service.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem('pymen.auth.session')).toBeNull();
  });
});
