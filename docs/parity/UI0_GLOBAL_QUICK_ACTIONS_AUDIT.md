# UI0 — Global quick actions parity audit

## Web source of truth

Desktop audit uses `npp-core/web/app/components/global-quick-actions.tsx` and
`global-quick-actions.module.css` as the reference.

The Web exposes exactly three shortcuts:

1. `/products` — **Sản phẩm**
2. `/customers` — **Khách hàng**
3. `/sales/sales-orders?quickAction=create` — **Tạo đơn bán**

All three links use `target="_blank"`. The sales workspace consumes
`quickAction=create`, checks `core.sales-order.create`, and opens the create
form. The global shortcut itself is not permission-filtered.

## Desktop audit before this slice

The Desktop shell already had full Product, Customer/Partner, and Sales
workspaces, including canonical idempotency handling in the existing view
models, but it had no global quick-action control and no equivalent to Web's
new-tab behavior.

## Desktop parity decision

A Web new tab maps to a **new WPF Window in the same application process**.
The child window contains only the requested feature workspace; it does not
instantiate another `MainWindow`, login screen, sidebar, or full app shell.

Each click creates a fresh feature view model so multiple product/customer/order
windows can be used independently:

- Sản phẩm -> `ProductViewModel` + `ProductView`
- Khách hàng -> `PartnerViewModel` + `PartnerView`, customer tab selected
- Tạo đơn bán -> `SalesViewModel` + `SalesView`, then quick-create editor

The windows are modeless (`Show`, never `ShowDialog`) so users can keep
several screens or several new sales orders open at the same time.

## Permissions and mutation contract

The three launchers remain visible like Web. Feature permissions are enforced by
the existing Desktop view models. Quick sales creation shows the same permission
message as Web when `core.sales-order.create` is missing.

No API, backend, database, or migration change is required. Existing canonical
idempotency-key behavior remains authoritative inside each independent view
model. A retry in the same editor reuses that editor's existing operation key;
opening a separate order window creates a separate user intent and therefore a
separate canonical key.
