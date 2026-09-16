import assert from 'node:assert/strict';
import { evaluateInventory, inventoryIdentity } from './parity-check.mjs';

const validStatus = 'planned';
const baseInventory = {
  fingerprints: {
    webAppTree: 'web-a',
    apiRoutesTree: 'api-a',
    accessTree: 'perm-a',
    serverBlob: 'server-a',
    contractsJsBlob: 'contract-js-a',
    contractsTypesBlob: 'contract-dts-a',
    idempotencyBlob: 'idem-a',
  },
  screens: [{ screenId: 'web:/sales', route: '/sales', sourceFile: 'web/sales/page.tsx', status: validStatus }],
  webRoutes: [{ route: '/sales', kind: 'screen', sourceFile: 'web/sales/page.tsx', status: validStatus }],
  apiSourceFiles: [{ sourceFile: 'api/routes/sales.js', status: validStatus }],
  endpoints: [{ method: 'GET', path: '/api/sales', sourceFile: 'api/routes/sales.js', sourceLine: 10, status: validStatus }],
  permissions: [{ key: 'core.sales.read', sourceFile: 'api/access/permissions.js', status: validStatus }],
  mutationCandidates: [{
    method: 'UNKNOWN',
    path: '/api/sales/:param',
    sourceFile: 'api/routes/sales.js',
    sourceLine: 20,
    status: validStatus,
    mappingStatus: validStatus,
  }],
};

function manifest(fingerprints = {}, snapshot = {}) {
  return { source: { fingerprints }, snapshot };
}

const baseManifests = {
  'SCREEN_MANIFEST.json': manifest(
    { webAppTree: 'web-a' },
    { screens: inventoryIdentity(baseInventory.screens, ['screenId', 'route', 'sourceFile']) },
  ),
  'WEB_ROUTE_MANIFEST.json': manifest(
    { webAppTree: 'web-a' },
    { webRoutes: inventoryIdentity(baseInventory.webRoutes, ['route', 'kind', 'sourceFile']) },
  ),
  'BACKEND_API_MANIFEST.json': manifest(
    {
      apiRoutesTree: 'api-a',
      serverBlob: 'server-a',
      contractsJsBlob: 'contract-js-a',
      contractsTypesBlob: 'contract-dts-a',
      idempotencyBlob: 'idem-a',
    },
    {
      apiRouteSources: inventoryIdentity(baseInventory.apiSourceFiles, ['sourceFile']),
      endpointCandidates: inventoryIdentity(baseInventory.endpoints, ['method', 'path', 'sourceFile', 'sourceLine']),
      mutationCandidates: inventoryIdentity(baseInventory.mutationCandidates, ['method', 'path', 'sourceFile', 'sourceLine']),
    },
  ),
  'PERMISSION_MANIFEST.json': manifest(
    { accessTree: 'perm-a' },
    { permissions: inventoryIdentity(baseInventory.permissions, ['key', 'sourceFile']) },
  ),
  'DESKTOP_PARITY_MATRIX.json': manifest(),
};

function codes(inventory, manifests = baseManifests) {
  return new Set(evaluateInventory(inventory, manifests).map((item) => item.code));
}

assert.equal(codes(baseInventory).size, 0, 'unchanged baseline must pass');

const webDrift = structuredClone(baseInventory);
webDrift.fingerprints.webAppTree = 'web-b';
assert(codes(webDrift).has('UNCLASSIFIED_WEB_ROUTE'));
assert(codes(webDrift).has('UNCLASSIFIED_COMPANY_SCREEN'));

const screenSetDrift = structuredClone(baseInventory);
screenSetDrift.screens.push({ screenId: 'web:/new', route: '/new', sourceFile: 'web/new/page.tsx', status: validStatus });
assert(codes(screenSetDrift).has('UNCLASSIFIED_COMPANY_SCREEN'));

const routeSetDrift = structuredClone(baseInventory);
routeSetDrift.webRoutes.push({ route: '/new', kind: 'screen', sourceFile: 'web/new/page.tsx', status: validStatus });
assert(codes(routeSetDrift).has('UNCLASSIFIED_WEB_ROUTE'));

const apiDrift = structuredClone(baseInventory);
apiDrift.fingerprints.apiRoutesTree = 'api-b';
const apiCodes = codes(apiDrift);
assert(apiCodes.has('UNCLASSIFIED_BACKEND_API'));
assert(apiCodes.has('API_CONTRACT_DRIFT'));
assert(apiCodes.has('UNMAPPED_MUTATION'));

const endpointSetDrift = structuredClone(baseInventory);
endpointSetDrift.endpoints.push({ method: 'GET', path: '/api/new', sourceFile: 'api/routes/new.js', sourceLine: 1, status: validStatus });
const endpointCodes = codes(endpointSetDrift);
assert(endpointCodes.has('UNCLASSIFIED_BACKEND_API'));
assert(endpointCodes.has('API_CONTRACT_DRIFT'));

const permissionDrift = structuredClone(baseInventory);
permissionDrift.fingerprints.accessTree = 'perm-b';
const permissionCodes = codes(permissionDrift);
assert(permissionCodes.has('UNCLASSIFIED_PERMISSION'));
assert(permissionCodes.has('PERMISSION_CONTRACT_DRIFT'));

const permissionSetDrift = structuredClone(baseInventory);
permissionSetDrift.permissions.push({ key: 'core.sales.write', sourceFile: 'api/access/permissions.js', status: validStatus });
const permissionSetCodes = codes(permissionSetDrift);
assert(permissionSetCodes.has('UNCLASSIFIED_PERMISSION'));
assert(permissionSetCodes.has('PERMISSION_CONTRACT_DRIFT'));

const idempotencyDrift = structuredClone(baseInventory);
idempotencyDrift.fingerprints.idempotencyBlob = 'idem-b';
assert(codes(idempotencyDrift).has('IDEMPOTENCY_CONTRACT_DRIFT'));

const contractDrift = structuredClone(baseInventory);
contractDrift.fingerprints.contractsJsBlob = 'contract-js-b';
assert(codes(contractDrift).has('API_CONTRACT_DRIFT'));

const unmapped = structuredClone(baseInventory);
unmapped.mutationCandidates[0].mappingStatus = '';
assert(codes(unmapped).has('UNMAPPED_MUTATION'));

const mutationSetDrift = structuredClone(baseInventory);
mutationSetDrift.mutationCandidates.push({
  method: 'POST',
  path: '/api/sales',
  sourceFile: 'api/routes/sales.js',
  sourceLine: 30,
  status: validStatus,
  mappingStatus: validStatus,
});
assert(codes(mutationSetDrift).has('UNMAPPED_MUTATION'));

console.log('Parity drift self-test PASS');
