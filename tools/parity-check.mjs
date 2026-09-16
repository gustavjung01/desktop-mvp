import fs from 'node:fs';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { pathToFileURL } from 'node:url';

const ERROR_CODES = Object.freeze({
  backend: 'UNCLASSIFIED_BACKEND_API',
  webRoute: 'UNCLASSIFIED_WEB_ROUTE',
  screen: 'UNCLASSIFIED_COMPANY_SCREEN',
  permission: 'UNCLASSIFIED_PERMISSION',
  apiDrift: 'API_CONTRACT_DRIFT',
  permissionDrift: 'PERMISSION_CONTRACT_DRIFT',
  idempotencyDrift: 'IDEMPOTENCY_CONTRACT_DRIFT',
  unmappedMutation: 'UNMAPPED_MUTATION',
});

const MUTATION_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);
const MANIFEST_NAMES = [
  'SCREEN_MANIFEST.json',
  'WEB_ROUTE_MANIFEST.json',
  'BACKEND_API_MANIFEST.json',
  'PERMISSION_MANIFEST.json',
  'DESKTOP_PARITY_MATRIX.json',
];
const ALLOWED_STATUSES = new Set(['implemented', 'planned', 'not_applicable', 'blocked']);

function normalizeSlash(value) {
  return value.replaceAll('\\', '/');
}

function listFiles(root) {
  if (!fs.existsSync(root)) return [];
  const result = [];
  const walk = (current) => {
    for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
      const full = path.join(current, entry.name);
      if (entry.isDirectory()) walk(full);
      else if (entry.isFile()) result.push(full);
    }
  };
  walk(root);
  return result.sort((a, b) => normalizeSlash(a).localeCompare(normalizeSlash(b)));
}

function relative(root, file) {
  return normalizeSlash(path.relative(root, file));
}

function gitObjectSha(sourceRoot, repoPath) {
  return execFileSync('git', ['-C', sourceRoot, 'rev-parse', `HEAD:${repoPath}`], { encoding: 'utf8' }).trim();
}

function readUtf8(sourceRoot, repoPath) {
  return fs.readFileSync(path.join(sourceRoot, ...repoPath.split('/')), 'utf8');
}

function deriveAppRoute(relativeAppFile) {
  let segments = normalizeSlash(relativeAppFile).split('/');
  segments = segments.slice(0, -1).filter((segment) => !(segment.startsWith('(') && segment.endsWith(')')));
  const routeSegments = segments.map((segment) => {
    if (segment.startsWith('[[...') && segment.endsWith(']]')) return `:${segment.slice(5, -2)}?`;
    if (segment.startsWith('[...') && segment.endsWith(']')) return `:${segment.slice(4, -1)}*`;
    if (segment.startsWith('[') && segment.endsWith(']')) return `:${segment.slice(1, -1)}`;
    return segment;
  });
  return `/${routeSegments.join('/')}`.replace(/\/$/, '') || '/';
}

function extractMethods(context) {
  const methods = new Set();
  const patterns = [
    /method\s*===\s*['"](GET|POST|PUT|PATCH|DELETE|OPTIONS|HEAD)['"]/gi,
    /req\.method\s*===\s*['"](GET|POST|PUT|PATCH|DELETE|OPTIONS|HEAD)['"]/gi,
    /new\s+Set\s*\(\s*\[([^\]]+)\]\s*\)/gi,
  ];
  for (const pattern of patterns) {
    for (const match of context.matchAll(pattern)) {
      if (match[1]?.includes("'") || match[1]?.includes('"')) {
        for (const nested of match[1].matchAll(/['"](GET|POST|PUT|PATCH|DELETE|OPTIONS|HEAD)['"]/gi)) {
          methods.add(nested[1].toUpperCase());
        }
      } else if (match[1]) {
        methods.add(match[1].toUpperCase());
      }
    }
  }
  return [...methods].sort();
}

function normalizeEndpointPath(value) {
  return value
    .replace(/\$\{[^}]+\}/g, ':param')
    .replace(/\\\//g, '/')
    .replace(/[),;]+$/g, '');
}

function extractEndpointCandidates(sourceFile, content) {
  const lines = content.split(/\r?\n/);
  const candidates = new Map();
  const routePattern = /(['"`])((?:\/api(?:\/[^'"`\s]*)?|\/health(?:\/[^'"`\s]*)?))\1/g;
  for (let index = 0; index < lines.length; index += 1) {
    for (const match of lineMatches(lines[index], routePattern)) {
      const endpointPath = normalizeEndpointPath(match[2]);
      if (!endpointPath.startsWith('/api') && !endpointPath.startsWith('/health')) continue;
      const start = Math.max(0, index - 4);
      const end = Math.min(lines.length, index + 5);
      const methods = extractMethods(lines.slice(start, end).join('\n'));
      const effectiveMethods = methods.length > 0 ? methods : ['UNKNOWN'];
      for (const method of effectiveMethods) {
        const key = `${method} ${endpointPath}`;
        if (!candidates.has(key)) {
          candidates.set(key, {
            method,
            path: endpointPath,
            sourceFile,
            sourceLine: index + 1,
            status: 'planned',
            authorityOwner: 'Công Ty backend',
            discovery: method === 'UNKNOWN' ? 'conservative-route-candidate' : 'method-near-route',
          });
        }
      }
    }
  }
  return [...candidates.values()];
}

function lineMatches(line, pattern) {
  pattern.lastIndex = 0;
  return [...line.matchAll(pattern)];
}

function extractPermissions(sourceFile, content) {
  const permissions = new Map();
  const pattern = /['"]((?:core|mcp)\.[a-z0-9]+(?:[._-][a-z0-9]+)+)['"]/gi;
  for (const match of content.matchAll(pattern)) {
    const key = match[1];
    if (!permissions.has(key)) permissions.set(key, { key, sourceFile, status: 'planned' });
  }
  return [...permissions.values()];
}

function loadManifest(manifestDir, name) {
  const full = path.join(manifestDir, name);
  if (!fs.existsSync(full)) throw new Error(`Missing required manifest: ${name}`);
  return JSON.parse(fs.readFileSync(full, 'utf8'));
}

export function loadManifests(manifestDir) {
  return Object.fromEntries(MANIFEST_NAMES.map((name) => [name, loadManifest(manifestDir, name)]));
}

export function inventoryIdentity(items, fields) {
  const identities = items
    .map((item) => fields.map((field) => String(item[field] ?? '')).join('|'))
    .sort((a, b) => a.localeCompare(b));
  return {
    count: identities.length,
    sha256: createHash('sha256').update(identities.join('\n'), 'utf8').digest('hex'),
  };
}

export function discoverInventory(sourceRoot) {
  const webAppRoot = path.join(sourceRoot, 'npp-core', 'web', 'app');
  const routeRoot = path.join(sourceRoot, 'npp-core', 'api', 'src', 'routes');
  const accessRoot = path.join(sourceRoot, 'npp-core', 'api', 'src', 'access');

  const screens = listFiles(webAppRoot)
    .filter((file) => path.basename(file) === 'page.tsx')
    .map((file) => ({
      screenId: `web:${deriveAppRoute(relative(webAppRoot, file))}`,
      route: deriveAppRoute(relative(webAppRoot, file)),
      sourceFile: relative(sourceRoot, file),
      status: 'planned',
      contractStatus: 'requires_business_lot_review',
    }));

  const webRoutes = listFiles(webAppRoot)
    .filter((file) => ['page.tsx', 'route.ts'].includes(path.basename(file)))
    .map((file) => ({
      route: deriveAppRoute(relative(webAppRoot, file)),
      kind: path.basename(file) === 'page.tsx' ? 'screen' : 'next-runtime-route',
      sourceFile: relative(sourceRoot, file),
      status: path.basename(file) === 'page.tsx' ? 'planned' : 'not_applicable',
      reason: path.basename(file) === 'page.tsx'
        ? 'Màn Công Ty hiện tại phải được ánh xạ sang Desktop theo lô nghiệp vụ.'
        : 'Desktop gọi Công Ty backend qua HTTPS, không dùng Next/Vercel làm runtime.',
    }));

  const apiSourceFiles = [
    'npp-core/api/src/server.js',
    ...listFiles(routeRoot).filter((file) => file.endsWith('.js')).map((file) => relative(sourceRoot, file)),
  ].sort();

  const endpoints = new Map();
  for (const sourceFile of apiSourceFiles) {
    const content = readUtf8(sourceRoot, sourceFile);
    for (const endpoint of extractEndpointCandidates(sourceFile, content)) {
      const key = `${endpoint.method} ${endpoint.path}`;
      const existing = endpoints.get(key);
      if (!existing || endpoint.sourceFile.localeCompare(existing.sourceFile) < 0) endpoints.set(key, endpoint);
    }
  }

  const permissions = new Map();
  for (const file of listFiles(accessRoot).filter((item) => item.endsWith('.js'))) {
    const sourceFile = relative(sourceRoot, file);
    const content = fs.readFileSync(file, 'utf8');
    for (const permission of extractPermissions(sourceFile, content)) {
      if (!permissions.has(permission.key)) permissions.set(permission.key, permission);
    }
  }

  const endpointList = [...endpoints.values()].sort((a, b) => `${a.path}|${a.method}`.localeCompare(`${b.path}|${b.method}`));
  const mutationCandidates = endpointList
    .filter((endpoint) => MUTATION_METHODS.has(endpoint.method) || endpoint.method === 'UNKNOWN')
    .map((endpoint) => ({
      ...endpoint,
      mutationKind: endpoint.method === 'UNKNOWN' ? 'potential_mutation' : 'mutation',
      mappingStatus: 'planned',
      idempotencyStatus: 'requires_endpoint_review_before_implementation',
    }));

  return {
    fingerprints: {
      webAppTree: gitObjectSha(sourceRoot, 'npp-core/web/app'),
      apiRoutesTree: gitObjectSha(sourceRoot, 'npp-core/api/src/routes'),
      accessTree: gitObjectSha(sourceRoot, 'npp-core/api/src/access'),
      serverBlob: gitObjectSha(sourceRoot, 'npp-core/api/src/server.js'),
      contractsJsBlob: gitObjectSha(sourceRoot, 'packages/contracts/index.js'),
      contractsTypesBlob: gitObjectSha(sourceRoot, 'packages/contracts/index.d.ts'),
      idempotencyBlob: gitObjectSha(sourceRoot, 'npp-core/api/src/idempotency.js'),
    },
    screens,
    webRoutes,
    apiSourceFiles: apiSourceFiles.map((sourceFile) => ({ sourceFile, status: 'planned' })),
    endpoints: endpointList,
    permissions: [...permissions.values()].sort((a, b) => a.key.localeCompare(b.key)),
    mutationCandidates,
  };
}

function expectedFingerprints(manifests) {
  const result = {};
  for (const manifest of Object.values(manifests)) {
    for (const [key, value] of Object.entries(manifest.source?.fingerprints ?? {})) {
      if (result[key] && result[key] !== value) throw new Error(`Manifest fingerprint conflict for ${key}`);
      result[key] = value;
    }
  }
  return result;
}

function addError(errors, code, detail) {
  if (!errors.some((item) => item.code === code && item.detail === detail)) errors.push({ code, detail });
}

function assertCanonicalIdempotency(sourceRoot, errors) {
  const contracts = readUtf8(sourceRoot, 'packages/contracts/index.js');
  const backend = readUtf8(sourceRoot, 'npp-core/api/src/idempotency.js');
  const hasMax = /IDEMPOTENCY_KEY_MAX_LENGTH\s*=\s*128\b/.test(contracts);
  const hasPattern = /IDEMPOTENCY_KEY_PATTERN\s*=\s*\/\^\[A-Za-z0-9\._-\]\{1,128\}\$\//.test(contracts);
  const hasGenerator = /export\s+function\s+createIdempotencyKey\s*\(/.test(contracts);
  const backendUsesCanonical = /IDEMPOTENCY_KEY_PATTERN\s*}\s*from\s*['"]@npp\/contracts['"]/.test(backend);
  if (!(hasMax && hasPattern && hasGenerator && backendUsesCanonical)) {
    addError(errors, ERROR_CODES.idempotencyDrift, 'Canonical Idempotency-Key contract is no longer the shared [A-Za-z0-9._-], 1-128 contract.');
  }
}

function assertSnapshot({ errors, manifest, snapshotName, items, fields, errorCode, driftCode, label }) {
  const expected = manifest.snapshot?.[snapshotName];
  if (!expected || !Number.isInteger(expected.count) || typeof expected.sha256 !== 'string') {
    addError(errors, errorCode, `${label} snapshot is missing from its manifest.`);
    if (driftCode) addError(errors, driftCode, `${label} snapshot contract is missing.`);
    return;
  }
  const actual = inventoryIdentity(items, fields);
  if (actual.count !== expected.count || actual.sha256 !== expected.sha256) {
    addError(errors, errorCode, `${label} inventory changed: expected ${expected.count}/${expected.sha256}, got ${actual.count}/${actual.sha256}.`);
    if (driftCode) addError(errors, driftCode, `${label} identity set drifted.`);
  }
}

export function evaluateInventory(inventory, manifests, sourceRoot = null) {
  const errors = [];
  const expected = expectedFingerprints(manifests);
  const actual = inventory.fingerprints;
  const changed = (key) => expected[key] && actual[key] !== expected[key];

  if (changed('webAppTree')) {
    addError(errors, ERROR_CODES.webRoute, `npp-core/web/app changed: expected ${expected.webAppTree}, got ${actual.webAppTree}`);
    addError(errors, ERROR_CODES.screen, 'Current Công Ty screen inventory changed and must be reclassified.');
  }
  if (changed('apiRoutesTree') || changed('serverBlob')) {
    addError(errors, ERROR_CODES.backend, 'Công Ty backend route surface changed and must be reclassified.');
    addError(errors, ERROR_CODES.apiDrift, 'Backend route source fingerprint changed.');
    addError(errors, ERROR_CODES.unmappedMutation, 'Mutation surface may have changed; update Desktop mutation mapping before merge.');
  }
  if (changed('contractsJsBlob') || changed('contractsTypesBlob')) {
    addError(errors, ERROR_CODES.apiDrift, 'Shared API envelope/contract source changed.');
  }
  if (changed('accessTree')) {
    addError(errors, ERROR_CODES.permission, 'Permission source changed and must be reclassified.');
    addError(errors, ERROR_CODES.permissionDrift, 'Permission contract fingerprint changed.');
  }
  if (changed('idempotencyBlob')) {
    addError(errors, ERROR_CODES.idempotencyDrift, 'Backend idempotency implementation changed.');
  }

  assertSnapshot({
    errors,
    manifest: manifests['SCREEN_MANIFEST.json'],
    snapshotName: 'screens',
    items: inventory.screens,
    fields: ['screenId', 'route', 'sourceFile'],
    errorCode: ERROR_CODES.screen,
    label: 'Công Ty screen',
  });
  assertSnapshot({
    errors,
    manifest: manifests['WEB_ROUTE_MANIFEST.json'],
    snapshotName: 'webRoutes',
    items: inventory.webRoutes,
    fields: ['route', 'kind', 'sourceFile'],
    errorCode: ERROR_CODES.webRoute,
    label: 'Web route',
  });
  assertSnapshot({
    errors,
    manifest: manifests['BACKEND_API_MANIFEST.json'],
    snapshotName: 'apiRouteSources',
    items: inventory.apiSourceFiles,
    fields: ['sourceFile'],
    errorCode: ERROR_CODES.backend,
    driftCode: ERROR_CODES.apiDrift,
    label: 'Backend API source',
  });
  assertSnapshot({
    errors,
    manifest: manifests['BACKEND_API_MANIFEST.json'],
    snapshotName: 'endpointCandidates',
    items: inventory.endpoints,
    fields: ['method', 'path', 'sourceFile', 'sourceLine'],
    errorCode: ERROR_CODES.backend,
    driftCode: ERROR_CODES.apiDrift,
    label: 'Backend endpoint candidate',
  });
  assertSnapshot({
    errors,
    manifest: manifests['PERMISSION_MANIFEST.json'],
    snapshotName: 'permissions',
    items: inventory.permissions,
    fields: ['key', 'sourceFile'],
    errorCode: ERROR_CODES.permission,
    driftCode: ERROR_CODES.permissionDrift,
    label: 'Permission',
  });
  assertSnapshot({
    errors,
    manifest: manifests['BACKEND_API_MANIFEST.json'],
    snapshotName: 'mutationCandidates',
    items: inventory.mutationCandidates,
    fields: ['method', 'path', 'sourceFile', 'sourceLine'],
    errorCode: ERROR_CODES.unmappedMutation,
    label: 'Mutation candidate',
  });

  if (sourceRoot) assertCanonicalIdempotency(sourceRoot, errors);

  if (inventory.screens.some((item) => !ALLOWED_STATUSES.has(item.status))) {
    addError(errors, ERROR_CODES.screen, 'At least one Công Ty screen has no valid classification.');
  }
  if (inventory.webRoutes.some((item) => !ALLOWED_STATUSES.has(item.status))) {
    addError(errors, ERROR_CODES.webRoute, 'At least one web route has no valid classification.');
  }
  if (inventory.apiSourceFiles.some((item) => !ALLOWED_STATUSES.has(item.status))
      || inventory.endpoints.some((item) => !ALLOWED_STATUSES.has(item.status))) {
    addError(errors, ERROR_CODES.backend, 'At least one backend API source/candidate has no valid classification.');
  }
  if (inventory.permissions.some((item) => !ALLOWED_STATUSES.has(item.status))) {
    addError(errors, ERROR_CODES.permission, 'At least one permission has no valid classification.');
  }
  if (inventory.mutationCandidates.some((item) => !item.mappingStatus)) {
    addError(errors, ERROR_CODES.unmappedMutation, 'At least one mutation or unresolved route candidate has no Desktop mapping status.');
  }

  return errors;
}

function buildDesktopMappings(inventory) {
  return {
    screens: inventory.screens.map((item) => ({ source: item.screenId, desktopStatus: 'planned', target: item.route })),
    webRoutes: inventory.webRoutes.map((item) => ({
      source: `${item.kind} ${item.route}`,
      desktopStatus: item.status,
      target: item.kind === 'screen' ? item.route : null,
    })),
    backendEndpoints: inventory.endpoints.map((item) => ({
      source: `${item.method} ${item.path}`,
      desktopStatus: 'planned',
      authorityOwner: 'Công Ty backend',
    })),
    permissions: inventory.permissions.map((item) => ({
      source: item.key,
      desktopStatus: 'planned',
      authorityOwner: 'Công Ty backend',
    })),
    mutationCandidates: inventory.mutationCandidates.map((item) => ({
      source: `${item.method} ${item.path}`,
      kind: item.mutationKind,
      desktopStatus: item.mappingStatus,
      idempotencyStatus: item.idempotencyStatus,
      authorityOwner: 'Công Ty backend',
    })),
  };
}

export function buildReport(inventory, manifests, errors) {
  const sourceRevision = manifests['SCREEN_MANIFEST.json'].source.baselineRevision;
  return {
    schemaVersion: 2,
    sourceRepository: 'binhnxwjfjxm/NPP-Platform',
    baselineRevision: sourceRevision,
    generatedAt: new Date().toISOString(),
    result: errors.length === 0 ? 'pass' : 'fail',
    errors,
    counts: {
      screens: inventory.screens.length,
      webRoutes: inventory.webRoutes.length,
      apiRouteSources: inventory.apiSourceFiles.length,
      endpointCandidates: inventory.endpoints.length,
      unresolvedEndpointMethods: inventory.endpoints.filter((item) => item.method === 'UNKNOWN').length,
      permissions: inventory.permissions.length,
      mutationCandidates: inventory.mutationCandidates.length,
    },
    fingerprints: inventory.fingerprints,
    snapshots: {
      screens: inventoryIdentity(inventory.screens, ['screenId', 'route', 'sourceFile']),
      webRoutes: inventoryIdentity(inventory.webRoutes, ['route', 'kind', 'sourceFile']),
      apiRouteSources: inventoryIdentity(inventory.apiSourceFiles, ['sourceFile']),
      endpointCandidates: inventoryIdentity(inventory.endpoints, ['method', 'path', 'sourceFile', 'sourceLine']),
      permissions: inventoryIdentity(inventory.permissions, ['key', 'sourceFile']),
      mutationCandidates: inventoryIdentity(inventory.mutationCandidates, ['method', 'path', 'sourceFile', 'sourceLine']),
    },
    classifications: {
      screens: inventory.screens,
      webRoutes: inventory.webRoutes,
      apiRouteSources: inventory.apiSourceFiles,
      backendEndpointCandidates: inventory.endpoints,
      permissions: inventory.permissions,
      mutationCandidates: inventory.mutationCandidates,
      desktopMappings: buildDesktopMappings(inventory),
    },
  };
}

function parseArgs(argv) {
  const result = {};
  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index];
    if (arg.startsWith('--')) result[arg.slice(2)] = argv[index + 1], index += 1;
  }
  return result;
}

export function runParityCheck({ sourceRoot, manifestDir, reportPath }) {
  const manifests = loadManifests(manifestDir);
  const inventory = discoverInventory(sourceRoot);
  const errors = evaluateInventory(inventory, manifests, sourceRoot);
  const report = buildReport(inventory, manifests, errors);
  fs.mkdirSync(path.dirname(reportPath), { recursive: true });
  fs.writeFileSync(reportPath, `${JSON.stringify(report, null, 2)}\n`, 'utf8');
  return report;
}

function main() {
  const args = parseArgs(process.argv.slice(2));
  const sourceRoot = path.resolve(args.source ?? '_source/NPP-Platform');
  const manifestDir = path.resolve(args.manifests ?? 'docs/parity');
  const reportPath = path.resolve(args.report ?? 'artifacts/parity-report.json');
  const report = runParityCheck({ sourceRoot, manifestDir, reportPath });
  console.log(`Parity inventory: ${report.counts.screens} screens, ${report.counts.webRoutes} web routes, ${report.counts.apiRouteSources} API source files, ${report.counts.endpointCandidates} endpoint candidates, ${report.counts.permissions} permissions, ${report.counts.mutationCandidates} mutation candidates.`);
  console.log(`Conservative unresolved endpoint methods: ${report.counts.unresolvedEndpointMethods}`);
  console.log(`Report: ${reportPath}`);
  if (report.errors.length > 0) {
    for (const error of report.errors) console.error(`${error.code}: ${error.detail}`);
    process.exitCode = 1;
  } else {
    console.log('Desktop parity baseline PASS');
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) main();
