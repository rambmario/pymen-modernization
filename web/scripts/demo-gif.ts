/**
 * Record a GIF of: login → deliver OT → wait for worker PDF → download remito.
 * Requires full compose stack (web :8088, API :5207, worker + Service Bus + Azurite).
 *
 * Usage (from web/): npm run demo:gif
 * Password: DEMO_PASSWORD_PLANNER from repo-root .env
 *
 * Output: docs/images/demo-remito.gif (+ temporary webm cleaned up)
 */
import { chromium, type Page } from 'playwright';
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const webRoot = path.resolve(__dirname, '..');
const repoRoot = path.resolve(webRoot, '..');
const imagesDir = path.join(repoRoot, 'docs', 'images');
const apiBase = process.env.PYMEN_API_URL ?? 'http://localhost:5207';
const appBase = process.env.PYMEN_WEB_URL ?? 'http://localhost:8088';
const plannerEmail = 'planner.t1@demo.local';

interface LoginResponse {
  accessToken: string;
}

interface PagedResult<T> {
  items: T[];
}

interface WorkOrderResponse {
  id: string;
  number: string;
  status: string;
}

interface CustomerResponse {
  id: string;
  isActive: boolean;
}

interface ProductResponse {
  id: string;
  isRawMaterial: boolean;
  isActive: boolean;
}

interface OperationTypeResponse {
  id: string;
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

  const password = loadEnvFile(path.join(repoRoot, '.env')).DEMO_PASSWORD_PLANNER?.trim();
  if (!password) {
    throw new Error('DEMO_PASSWORD_PLANNER missing in repo-root .env');
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

async function createInProductionWorkOrder(token: string): Promise<WorkOrderResponse> {
  const customers = await apiJson<CustomerResponse[]>(`${apiBase}/api/customers`, { token });
  const products = await apiJson<ProductResponse[]>(`${apiBase}/api/products`, { token });
  const operationTypes = await apiJson<OperationTypeResponse[]>(
    `${apiBase}/api/operation-types`,
    { token },
  );

  const customerId = customers.find((c) => c.isActive)?.id;
  const productId = products.find((p) => p.isActive && !p.isRawMaterial)?.id;
  const opId = operationTypes[0]?.id;
  if (!customerId || !productId || !opId) {
    throw new Error('Demo catalog incomplete');
  }

  const created = await apiJson<WorkOrderResponse>(`${apiBase}/api/work-orders`, {
    method: 'POST',
    token,
    body: JSON.stringify({
      customerId,
      notes: 'Demo GIF — deliver → PDF',
      lines: [{ productId, quantity: 2 }],
      operations: [{ operationTypeId: opId }],
    }),
  });

  return apiJson<WorkOrderResponse>(`${apiBase}/api/work-orders/${created.id}/start`, {
    method: 'POST',
    token,
  });
}

async function waitForPdf(token: string, workOrderId: string, timeoutMs = 90_000): Promise<void> {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const response = await fetch(`${apiBase}/api/work-orders/${workOrderId}/delivery-note.pdf`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (response.ok) {
      return;
    }
    if (response.status !== 404) {
      throw new Error(`PDF poll → ${response.status}`);
    }
    await new Promise((r) => setTimeout(r, 1500));
  }
  throw new Error('Timed out waiting for delivery-note PDF (is the worker running?)');
}

function convertWebmToGif(webmPath: string, gifPath: string): void {
  const vf = 'fps=8,scale=960:-1:flags=lanczos';
  const local = spawnSync(
    'ffmpeg',
    ['-y', '-i', webmPath, '-vf', vf, '-loop', '0', gifPath],
    { encoding: 'utf8' },
  );
  if (local.status === 0) {
    console.log('Converted with local ffmpeg');
    return;
  }

  const docker = spawnSync(
    'docker',
    [
      'run',
      '--rm',
      '-v',
      `${imagesDir}:/work`,
      'jrottenberg/ffmpeg:6.1-alpine',
      '-y',
      '-i',
      `/work/${path.basename(webmPath)}`,
      '-vf',
      vf,
      '-loop',
      '0',
      `/work/${path.basename(gifPath)}`,
    ],
    { encoding: 'utf8' },
  );
  if (docker.status !== 0) {
    console.error(docker.stdout);
    console.error(docker.stderr);
    throw new Error('ffmpeg conversion failed (install ffmpeg or allow Docker pull jrottenberg/ffmpeg)');
  }
  console.log('Converted with Docker ffmpeg');
}

async function pause(page: Page, ms: number): Promise<void> {
  await page.waitForTimeout(ms);
}

async function main(): Promise<void> {
  const password = requirePlannerPassword();
  fs.mkdirSync(imagesDir, { recursive: true });

  console.log(`API ${apiBase} · Web ${appBase}`);
  const login = await apiJson<LoginResponse>(`${apiBase}/api/auth/login`, {
    method: 'POST',
    body: JSON.stringify({ email: plannerEmail, password }),
  });
  const wo = await createInProductionWorkOrder(login.accessToken);
  console.log(`Prepared InProduction OT ${wo.number} (${wo.id})`);

  const videoDir = path.join(imagesDir, '_demo-video');
  fs.rmSync(videoDir, { recursive: true, force: true });
  fs.mkdirSync(videoDir, { recursive: true });

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1280, height: 720 },
    recordVideo: { dir: videoDir, size: { width: 1280, height: 720 } },
  });
  const page = await context.newPage();

  try {
    await page.goto(`${appBase}/login`, { waitUntil: 'networkidle' });
    await page.getByText('Pymen', { exact: true }).first().waitFor({ state: 'visible' });
    await pause(page, 800);

    await page.locator('input[type="email"]').fill(plannerEmail);
    await page.locator('input[type="password"]').fill(password);
    await pause(page, 400);
    await page.getByRole('button', { name: 'Entrar' }).click();
    await page.waitForURL(/\/work-orders/, { timeout: 30_000 });
    await page.locator('table.mat-mdc-table, table[mat-table]').waitFor({ state: 'visible' });
    await pause(page, 1000);

    await page.goto(`${appBase}/work-orders/${wo.id}`, { waitUntil: 'networkidle' });
    await page.getByRole('heading', { name: /^OT / }).waitFor({ state: 'visible' });
    await pause(page, 1000);

    await page.getByRole('button', { name: 'Entregar' }).click();
    await page.getByText(/Entregada|Delivered|Descargar remito/i).first().waitFor({
      state: 'visible',
      timeout: 30_000,
    });
    await pause(page, 800);

    console.log('Waiting for worker PDF…');
    await waitForPdf(login.accessToken, wo.id);
    await page.reload({ waitUntil: 'networkidle' });
    await page.getByRole('button', { name: 'Descargar remito' }).waitFor({ state: 'visible' });
    await pause(page, 600);

    const downloadPromise = page.waitForEvent('download', { timeout: 30_000 });
    await page.getByRole('button', { name: 'Descargar remito' }).click();
    const download = await downloadPromise;
    const pdfPath = path.join(imagesDir, '_demo-remito.pdf');
    await download.saveAs(pdfPath);
    console.log(`Downloaded ${path.basename(pdfPath)}`);
    await pause(page, 1200);
  } finally {
    await context.close();
    await browser.close();
  }

  const webm = fs.readdirSync(videoDir).find((f) => f.endsWith('.webm'));
  if (!webm) {
    throw new Error('Playwright did not produce a .webm recording');
  }

  const webmPath = path.join(videoDir, webm);
  const stagedWebm = path.join(imagesDir, 'demo-remito.webm');
  fs.copyFileSync(webmPath, stagedWebm);

  const gifPath = path.join(imagesDir, 'demo-remito.gif');
  convertWebmToGif(stagedWebm, gifPath);

  fs.rmSync(videoDir, { recursive: true, force: true });
  fs.rmSync(stagedWebm, { force: true });
  fs.rmSync(path.join(imagesDir, '_demo-remito.pdf'), { force: true });

  console.log(`Wrote docs/images/demo-remito.gif`);
}

main().catch((error: unknown) => {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
});
