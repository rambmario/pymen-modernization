import { WorkOrderNumberPipe } from './work-order-number.pipe';

describe('WorkOrderNumberPipe', () => {
  const pipe = new WorkOrderNumberPipe();

  it('formats a 12-digit number as POS-sequence', () => {
    expect(pipe.transform('000100000002')).toBe('0001-00000002');
  });

  it('leaves already-formatted values unchanged', () => {
    expect(pipe.transform('0001-00000002')).toBe('0001-00000002');
  });

  it('returns empty for nullish input', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
  });

  it('passes through non-standard values', () => {
    expect(pipe.transform('OT-100')).toBe('OT-100');
  });
});
