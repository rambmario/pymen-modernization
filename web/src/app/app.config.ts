import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { MAT_DATE_LOCALE } from '@angular/material/core';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { APP_LOCALE, ensureAppLocale } from './core/i18n/locale';
import { SpanishPaginatorIntl } from './core/i18n/spanish-paginator-intl';

ensureAppLocale();

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimationsAsync(),
    { provide: LOCALE_ID, useValue: APP_LOCALE },
    { provide: MAT_DATE_LOCALE, useValue: APP_LOCALE },
    { provide: MatPaginatorIntl, useClass: SpanishPaginatorIntl },
  ],
};
