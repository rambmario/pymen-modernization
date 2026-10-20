import { SpanishPaginatorIntl } from './spanish-paginator-intl';

describe('SpanishPaginatorIntl', () => {
  const intl = new SpanishPaginatorIntl();

  it('uses Spanish labels', () => {
    expect(intl.itemsPerPageLabel).toBe('Ítems por página:');
    expect(intl.nextPageLabel).toBe('Página siguiente');
    expect(intl.previousPageLabel).toBe('Página anterior');
  });

  it('formats the range label in Spanish', () => {
    expect(intl.getRangeLabel(0, 10, 3)).toBe('1 – 3 de 3');
    expect(intl.getRangeLabel(0, 10, 0)).toBe('0 de 0');
  });
});
