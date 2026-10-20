import { registerLocaleData } from '@angular/common';
import localeEsAr from '@angular/common/locales/es-AR';

/** Application locale identifier (Argentina Spanish). */
export const APP_LOCALE = 'es-AR';

let registered = false;

/** Idempotent registration for app bootstrap and unit tests. */
export function ensureAppLocale(): void {
  if (registered) {
    return;
  }
  registerLocaleData(localeEsAr);
  registered = true;
}
