/**
 * Capture README screenshots against a local Dev stack.
 * Requires: API on :5207, SPA on :4200, docker SQL + seed.
 *
 * Usage (from web/): npm run screenshots
 * Password: DEMO_PASSWORD_PLANNER from repo-root .env (never hardcoded).
 */
import { chromium, type Page } from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const webRoot = path.resolve(__dirname, '..');
const repoRoot = path.resolve(webRoot, '..');
const imagesDir = path.join(repoRoot, 'docs', 'images');
const apiBase = process.env.PYMEN_API_URL ?? 'http://localhost:5207';
const appBase = process.env.PYMEN_WEB_URL ?? 'http://localhost:4200';
const plannerEmail = 'planner.t1@demo.local';

interface LoginResponse {
  accessToken: string;
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

interface WorkOrderResponse {
  id: string;
  number: string;
  status: string;
  operations: { id: string; status: string }[];
}

interface CustomerResponse {
  id: string;
  name: string;
  isActive: boolean;
}

interface ProductResponse {
  id: string;
  sku: string;
  isRawMaterial: boolean;
  isActive: boolean;
}

interface OperationTypeResponse {
  id: string;
  code: string;
}

function loadEnvFile(filePath: string): Record<string, string> {
  if (!fs.existsSync(filePath)) {
    return {};
  }

  const values: Record<string, string> = {};
  for (const line of fs.readFileSync(filePath, 'utf8').split(/\r?\n/)) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('#')) {
      continue;
    }
    const eq = trimmed.indexOf('=');
    if (eq <= 0) {
      continue;
    }
    const key = trimmed.slice(0, eq).trim();
    let value = trimmed.slice(eq + 1).trim();
    if (
      (value.startsWith('"') && value.endsWith('"')) ||
      (value.startsWith("'") && value.endsWith("'"))
    ) {
      value = value.slice(1, -1);
    }
    values[key] = value;
  }
  return values;
}

function requirePlannerPassword(): string {
  const fromProcess = process.env.DEMO_PASSWORD_PLANNER?.trim();
  if (fromProcess) {
    return fromProcess;
  }

  const env = loadEnvFile(path.join(repoRoot, '.env'));
  const password = env.DEMO_PASSWORD_PLANNER?.trim();
  if (!password) {
    throw new Error(
      'DEMO_PASSWORD_PLANNER is missing. Set it in the repo-root .env (see .env.example).',
    );
  }
  return password;
}

async function apiJson<T>(
  url: string,
  init: RequestInit & { token?: string } = {},
): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set('Accept', 'application/json');
  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }
  if (init.token) {
    headers.set('Authorization', `Bearer ${init.token}`);
  }

  const response = await fetch(url, { ...init, headers });
  if (!response.ok) {
    const body = await response.text();
    throw new Error(`${init.method ?? 'GET'} ${url} → ${response.status}: ${body}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function loginApi(password: string): Promise<string> {
  const login = await apiJson<LoginResponse>(`${apiBase}/api/auth/login`, {
    method: 'POST',
    body: JSON.stringify({ email: plannerEmail, password }),
  });
  return login.accessToken;
}

async function prepareDemoData(token: string): Promise<string> {
  const page = await apiJson<PagedResult<WorkOrderResponse>>(
    `${apiBase}/api/work-orders?page=1&pageSize=50`,
    { token },
  );

  const byStatus = (status: string) => page.items.filter((w) => w.status === status);
  let drafts = byStatus('Draft');
  let inProduction = byStatus('InProduction');
  let delivered = byStatus('Delivered');

  const customers = await apiJson<CustomerResponse[]>(`${apiBase}/api/customers`, { token });
  const products = await apiJson<ProductResponse[]>(`${apiBase}/api/products`, { token });
  const operationTypes = await apiJson<OperationTypeResponse[]>(
    `${apiBase}/api/operation-types`,
    { token },
  );

  const customerId = customers.find((c) => c.isActive)?.id;
  const productId = products.find((p) => p.isActive && !p.isRawMaterial)?.id;
  const opIds = operationTypes.map((o) => o.id);

  if (!customerId || !productId || opIds.length < 2) {
    throw new Error('Demo catalog incomplete (customer/product/operation types).');
  }

  async function createWo(notes: string, withOps: boolean): Promise<WorkOrderResponse> {
    return apiJson<WorkOrderResponse>(`${apiBase}/api/work-orders`, {
      method: 'POST',
      token,
      body: JSON.stringify({
        customerId,
        notes,
        lines: [{ productId, quantity: 3 }],
        operations: withOps
          ? [{ operationTypeId: opIds[0] }, { operationTypeId: opIds[1] }]
          : [{ operationTypeId: opIds[0] }],
      }),
    });
  }

  if (drafts.length === 0) {
    drafts = [await createWo('Screenshot demo — Draft', true)];
  }

  if (inProduction.length === 0) {
    const created = await createWo('Screenshot demo — InProduction', true);
    inProduction = [
      await apiJson<WorkOrderResponse>(`${apiBase}/api/work-orders/${created.id}/start`, {
        method: 'POST',
        token,
      }),
    ];
  }

  if (delivered.length === 0) {
    const created = await createWo('Screenshot demo — Delivered', false);
    const started = await apiJson<WorkOrderResponse>(
      `${apiBase}/api/work-orders/${created.id}/start`,
      { method: 'POST', token },
    );
    delivered = [
      await apiJson<WorkOrderResponse>(`${apiBase}/api/work-orders/${started.id}/deliver`, {
        method: 'POST',
        token,
      }),
    ];
  }

  // Detail target: InProduction with one Finished + one Started operation.
  let detail = inProduction[0];
  detail = await apiJson<WorkOrderResponse>(`${apiBase}/api/work-orders/${detail.id}`, {
    token,
  });

  if (detail.operations.length < 2) {
    const created = await createWo('Screenshot demo — ops detail', true);
    detail = await apiJson<WorkOrderResponse>(`${apiBase}/api/work-orders/${created.id}/start`, {
      method: 'POST',
      token,
    });
  }

  const ops = [...detail.operations];
  const first = ops[0];
  const second = ops[1];

  async function mutateOp(operationId: string, action: 'start' | 'finish'): Promise<void> {
    detail = await apiJson<WorkOrderResponse>(
      `${apiBase}/api/work-orders/${detail.id}/operations/${operationId}/${action}`,
      { method: 'POST', token },
    );
  }

  if (first.status === 'Pending') {
    await mutateOp(first.id, 'start');
  }
  const firstAfter = detail.operations.find((o) => o.id === first.id)!;
  if (firstAfter.status === 'Started') {
    await mutateOp(first.id, 'finish');
  }

  const secondLive = detail.operations.find((o) => o.id === second.id)!;
  if (secondLive.status === 'Pending') {
    await mutateOp(second.id, 'start');
  }

  detail = await apiJson<WorkOrderResponse>(`${apiBase}/api/work-orders/${detail.id}`, {
    token,
  });

  const statuses = detail.operations.map((o) => o.status);
  if (!statuses.includes('Finished') || !statuses.includes('Started')) {
    throw new Error(
      `Detail WO ${detail.number} ops not ready for screenshot: ${statuses.join(', ')}`,
    );
  }

  console.log(`Prepared detail work order ${detail.number} (${detail.id}).`);
  return detail.id;
}

async function waitForApp(page: Page): Promise<void> {
  await page.goto(`${appBase}/login`, { waitUntil: 'networkidle' });
  await page.getByText('Pymen', { exact: true }).first().waitFor({ state: 'visible' });
}

async function captureLogin(page: Page): Promise<void> {
  // Fresh navigation keeps the form empty without triggering validators.
  await waitForApp(page);
  await page.locator('input[type="email"]').waitFor({ state: 'visible' });
  await page.locator('input[type="password"]').waitFor({ state: 'visible' });
  await page.waitForTimeout(300);
  await page.screenshot({
    path: path.join(imagesDir, 'login.png'),
    fullPage: false,
  });
  console.log('Wrote docs/images/login.png');
}

async function loginUi(page: Page, password: string): Promise<void> {
  await page.locator('input[type="email"]').fill(plannerEmail);
  await page.locator('input[type="password"]').fill(password);
  await page.getByRole('button', { name: 'Entrar' }).click();
  await page.waitForURL(/\/work-orders/, { timeout: 30_000 });
  await page.locator('table.mat-mdc-table, table[mat-table]').waitFor({ state: 'visible' });
}

async function captureList(page: Page): Promise<void> {
  await page.getByRole('heading', { name: 'Órdenes de trabajo' }).waitFor({ state: 'visible' });
  await page.locator('mat-form-field').first().waitFor({ state: 'visible' });
  await page.locator('tr.mat-mdc-row, tr[mat-row]').first().waitFor({ state: 'visible' });
  await page.waitForTimeout(400);
  await page.screenshot({
    path: path.join(imagesDir, 'work-order-list.png'),
    fullPage: false,
  });
  console.log('Wrote docs/images/work-order-list.png');
}

async function captureDetail(page: Page, workOrderId: string): Promise<void> {
  await page.goto(`${appBase}/work-orders/${workOrderId}`, { waitUntil: 'networkidle' });
  await page.getByRole('heading', { name: /^OT / }).waitFor({ state: 'visible' });
  await page.getByRole('heading', { name: 'Operaciones' }).waitFor({ state: 'visible' });
  await page.waitForTimeout(400);
  await page.screenshot({
    path: path.join(imagesDir, 'work-order-detail.png'),
    fullPage: false,
  });
  console.log('Wrote docs/images/work-order-detail.png');
}

async function main(): Promise<void> {
  const password = requirePlannerPassword();
  fs.mkdirSync(imagesDir, { recursive: true });

  console.log(`API ${apiBase} · Web ${appBase}`);
  const token = await loginApi(password);
  const detailId = await prepareDemoData(token);

  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({
    viewport: { width: 1400, height: 900 },
  });

  try {
    await captureLogin(page);
    await loginUi(page, password);
    // Clear password field is already navigated away; capture list without secrets in DOM.
    await captureList(page);
    await captureDetail(page, detailId);
  } finally {
    await browser.close();
  }

  console.log('Screenshots complete.');
}

main().catch((error: unknown) => {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
});
