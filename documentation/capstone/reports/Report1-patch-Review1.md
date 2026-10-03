# Report 1 — bản vá theo Review 1 (paste vào file .docx)

*Dùng các khối dưới đây sửa đúng mục tương ứng trong `Report1_Project Introduction.docx`. Mọi con số đã khớp bộ hồ sơ 30/09: 103 BR · 49 FR · 33 UC · 17 NFR core.*

## 1. Record of Changes — THÊM 1 dòng mới

| Date | A/M/D | In charge | Change Description |
|---|---|---|---|
| 30/9/2026 | M | Nguyen Xuan Tan | Incorporated supervisor Review 1 feedback (30/9): staged deposit payment, staff quote-review and lab-proof approval gates, file-hash evidence chain, shipment merging and lab→hub batch runs, category-aware assignment, design-service intake; features FE-04/06/07/08/09/10/11 amended, FE-13/14 added, LI-04 updated, LI-05–08 added. |

## 2. Section 6.1 Major Features — SỬA các dòng (thay nguyên văn)

**FE-04:** Receive an automatic quote — engine-drafted with cost breakdown, committed delivery date and expiry — reviewed and published by platform staff (auto-lane allowed for simple low-value orders); confirm the order by paying a deposit, with the balance settled before shipment, through a real online payment gateway.

**FE-06:** Lab registration and management of working calendar, machine registry, material/colour inventory with a transaction-coded ledger, job offers with accept/decline, and machine-level or workshop-level schedule visibility (Lab Manager).

**FE-07:** Job-queue execution, state progression, actual-duration and incident reporting, self-quality-check with photographic proof, and batched hub-handover recording for shop-floor operators (Lab Operator).

**FE-08:** Central Hub batch receipt and reconciliation, checklist-driven inspection, defect classification, fault attribution and reprint initiation (Hub Quality Control Staff).

**FE-09:** Order consolidation, automatic merging of multiple shipments for the same customer and address, packing, shipment recording and delivery-status updates (Hub Fulfillment Staff).

**FE-10:** Network-wide operations monitoring, at-risk job alerts, manual assignment/priority override with recorded justification, lab suspension/capping, offer arbitration, quote and proof review queues, SLA dashboards, partner settlement and revenue reports, and report export (Operations Manager).

**FE-11:** Automated slicing/geometry analysis with per-upload content hashing, network-uniform pricing engine, capability-filtered and category-aware multi-criteria assignment, offer fan-out with first-accept, backward due-date scheduling with batch consolidation and quantity splitting, speculative quote-time scheduling and event-driven rescheduling (Pricing and Scheduling Engine).

**FE-12:** System administration of accounts/roles/permissions, catalogues (materials, colours, technologies, quality grades), pricing/assignment/SLA/deposit configuration, partner discount rates, and audit-log review.

## 3. Section 6.1 — THÊM 2 tính năng mới (sau FE-12)

**FE-13 (Supervision & Evidence Chain):** Intellectual-property evidence for every model file — SHA-256 hash at upload, automatic confirmation email to the customer (time, size, hash), partner acknowledgement of the received file hash at job acceptance — plus staff review gates on quotes and on lab quality proofs, all recorded immutably for dispute reconstruction.

**FE-14 (Design-Service Intake):** Capture of custom requests from customers who have an idea but no 3D file — brief plus reference images, staged design quotation reusing the deposit/balance payment machinery, and delivery of a customer-owned model file that may never be reused by the platform or any lab (copyright retained by the customer).

## 4. Section 6.2 — SỬA LI-04 và THÊM LI-05..08

**LI-04 (sửa):** The estimate-calibration loop and the real-lab trial depend on a partnership with a real printing lab; one partner lab has been confirmed (23/9/2026), and the trial scope is limited by its availability and declared data access.

**LI-05:** Carrier integration is limited to manually recorded waybills; no API connection to shipping companies is in scope.

**LI-06:** Financial settlement with labs (payout = contracted base rate + transport allowance − reprint costs) is computed and displayed by the platform but executed off-platform through an operational runbook; the platform holds no lab wallets.

**LI-07:** FE-14 covers design-request intake and hand-over only; the design work itself (modelling labour) is delivered outside the platform through the operations runbook.

**LI-08:** Vouchers/promotional codes, multi-currency and tax invoicing are excluded pending final confirmation with the supervisor (open item B24).

## 5. Gợi ý không bắt buộc

- Mục 4 (Business Opportunity) thêm 1 câu về điểm kinh tế mới từ Review 1: *"Uniform shipping pricing with a bounded loss tolerance and a transport allowance for distant labs keeps both sides of the network contractually fair."*
- Mục 2 (Product Background) đã ổn, không sửa.
