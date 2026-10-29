# Report 3 — 3.1.1 Screens Flow (Mermaid, v2 — screen-level)

**Dữ liệu:** `frontend/src/routes/AppRoutes.tsx` + rà trực tiếp các page để xác định pop-up (`<Dialog>`, `<Drawer>`)
và màn nhiều tab (`<Tabs>`). Ngày 28/10/2026.

## Ký hiệu (dùng đúng theo yêu cầu của template)

| Ký hiệu trong sơ đồ | Cú pháp Mermaid | Ý nghĩa |
|---|---|---|
| Chữ nhật | `A["Model Library<br/>/models"]` | **Screen phẳng** (flat screen) |
| Oval / bo tròn | `A(["Upload Model<br/>pop-up"])` | **Pop-up** (Dialog, Drawer, panel nổi) |
| Chữ nhật viền đôi | `A[["Shared Catalog<br/>tabs: … "]]` | **Screen nhiều tab** (multi-tab screen) |
| Lục giác | `A{{"AnalyzeModelJob"}}` | **Xử lý nền** (async worker — không phải màn) |
| Thoi | `A{"Role?"}` | **Điểm rẽ nhánh** (routing / gate) |
| Chữ nhật nền xám, viền đứt | `classDef state` | **Trạng thái kết thúc / outcome** (không phải màn hình) |
| Chữ nhật viền xanh đứt | `classDef cross` | **Màn thuộc portal khác** (node tham chiếu, không vẽ lại) |
| Khung nền màu | `subgraph M01["…"]` | **Phân hệ** (background grouping) |
| Nét đứt `-.->` | `A -.-> B` | Điều hướng **gián tiếp**: qua notification/email, hoặc gọi async |

> **Lưu ý khi đưa vào Word:** template ghi "oval notation" — trong Mermaid hình gần nhất là *stadium*
> (chữ nhật bo tròn hai đầu). Khi vẽ lại bằng Visio/draw.io thì dùng ellipse thật; nếu dùng ảnh Mermaid
> thì ghi chú dưới caption: "oval/stadium = pop-up screen".

---

## Diagram 1 — Overall Screens Flow (hình chính cho mục 3.1.1)

Luồng chính xếp theo **một trục thời gian dọc** (vừa A4 portrait). Vì luồng nghiệp vụ có chu trình
(Quote Review nằm giữa Pricing và Checkout; QC Proof nằm giữa Lab và Shipping), giữ subgraph sẽ luôn
sinh cạnh chéo. Cách xử lý: **bỏ khung subgraph, dùng màu nền để phân biệt phân hệ** — đúng phần
"use background format for different visuality purpose" của template.

```mermaid
flowchart TB
    %% ── Public entry ─────────────────────────────
    HOM["Home<br/>/"]
    LOG["Login<br/>/login"]
    REG["Register<br/>/register"]
    VER["Verify Email<br/>/verify-email"]
    FGP["Forgot Password<br/>/forgot-password"]
    RSP["Reset Password<br/>/reset-password"]

    %% ── Customer ordering spine ──────────────────
    MOD["Model Library<br/>/models"]
    UPL(["Upload Model<br/>pop-up"])
    PRE["Model Preview<br/>/models/:id/preview"]
    CFG["Order Configuration<br/>/models/:id/order"]
    QPN(["Quote Panel<br/>breakdown · date · expiry"])
    CHK(["Checkout &amp; Deposit<br/>pop-up"])
    ORD["My Orders<br/>/orders"]
    ODD(["Order Details<br/>drawer"])

    %% ── Engine (background) ──────────────────────
    ANL{{"AnalyzeModelJob"}}
    PRC{{"Pricing + Speculative Placement"}}
    ASG{{"Assignment Engine"}}

    %% ── Approval gates (Ops staff) ───────────────
    QREV["Quote Review<br/>/quote-reviews"]
    QCPR["QC Proof Review<br/>/staff/qc-proofs"]

    %% ── Lab ──────────────────────────────────────
    LQU["Lab Queue<br/>/lab/queue"]
    LCP(["Complete Job<br/>pop-up"])
    LSB["Shipment Batch Planner<br/>/lab/:labId/shipments"]

    %% ── Hub ──────────────────────────────────────
    HSP["Hub Shipments<br/>/hub/shipments"]
    HQC["QC Console<br/>/hub/qc"]
    HCO["Complete Orders<br/>/hub/complete-orders"]

    DONE["Delivered<br/>30-day guarantee window"]

    %% ── Navigation ───────────────────────────────
    HOM --> LOG
    HOM --> REG
    REG --> VER
    VER --> LOG
    LOG --> FGP
    FGP -.->|"reset link"| RSP
    RSP --> LOG
    LOG --> MOD

    MOD --> UPL
    UPL --> ANL
    ANL -.->|"analysis stored"| MOD
    MOD --> PRE
    MOD -->|"Đặt in"| CFG
    PRE -->|"Đặt in"| CFG
    CFG --> QPN
    QPN --> PRC
    PRC --> QREV
    QREV -->|"PUBLISHED"| CHK
    CHK --> ORD
    ORD --> ODD

    CHK -.->|"order PAID"| ASG
    ASG -->|"job offer"| LQU
    LQU --> LCP
    LCP --> QCPR
    QCPR -->|"approved"| LSB
    LSB --> HSP
    HSP --> HQC
    HQC -->|"PASS"| HCO
    HQC -.->|"FAIL → reprint"| ASG
    HCO --> DONE

    classDef auth fill:#f0fdf4,stroke:#16a34a
    classDef cust fill:#eff6ff,stroke:#2563eb
    classDef eng fill:#eef2ff,stroke:#4f46e5,stroke-width:2px
    classDef ops fill:#fff7ed,stroke:#ea580c
    classDef lab fill:#ecfeff,stroke:#0891b2
    classDef hub fill:#f5f3ff,stroke:#7c3aed
    classDef popup fill:#fef3c7,stroke:#d97706,stroke-width:2px
    classDef state fill:#f1f5f9,stroke:#94a3b8,stroke-dasharray: 5 5
    class HOM,LOG,REG,VER,FGP,RSP auth
    class MOD,PRE,CFG,ORD cust
    class ANL,PRC,ASG eng
    class QREV,QCPR ops
    class LQU,LSB lab
    class HSP,HQC,HCO hub
    class UPL,QPN,CHK,ODD,LCP popup
    class DONE state
```

**Chú giải màu:** xanh lá = Public/Auth · xanh dương = Customer · chàm = Engine · cam = Ops ·
xanh ngọc = Lab · tím = Hub · vàng + oval = pop-up/drawer · xám viền đứt = trạng thái kết thúc.

**Không vẽ cạnh `Delivered → My Orders`** dù thực tế có cập nhật trạng thái realtime — để tránh một cạnh
kéo ngược từ cuối lên đầu sơ đồ. Ghi chú dưới hình: *"Order status is pushed to `My Orders` in real time
via SignalR (see 3.1.4 #15)."*


---

## Diagram 2 — Public & Authentication

```mermaid
flowchart TD
    HOM["Home<br/>/"]
    LOG["Login<br/>/login"]
    REG["Register<br/>/register"]
    VER["Verify Email<br/>/verify-email"]
    FGP["Forgot Password<br/>/forgot-password"]
    RSP["Reset Password<br/>/reset-password"]
    ROLE{"Role?"}

    D1["Model Library<br/>/models"]
    D2["Lab Queue<br/>/lab/queue"]
    D3["Hub Shipments<br/>/hub/shipments"]
    D4["Scheduling Board<br/>/scheduling"]
    D5["Admin Overview<br/>/admin"]

    HOM -->|"Đăng nhập"| LOG
    HOM -->|"Đăng ký"| REG
    REG -->|"verification email"| VER
    VER --> LOG
    LOG -->|"Quên mật khẩu"| FGP
    FGP -.->|"reset link (30 phút, 1 lần)"| RSP
    RSP --> LOG
    LOG --> ROLE
    ROLE -->|Customer| D1
    ROLE -->|"LabManager / LabOperator"| D2
    ROLE -->|"HubQC / HubFulfillment"| D3
    ROLE -->|"OpsManager / Admin"| D4
    ROLE -->|Admin| D5

    classDef popup fill:#fef3c7,stroke:#d97706
    classDef decision fill:#f1f5f9,stroke:#475569
    class ROLE decision
```

---

## Diagram 3 — Customer Portal (M01), đầy đủ

```mermaid
flowchart TD
    MOD["Model Library<br/>/models · /library"]
    UPL(["Upload Model<br/>pop-up"])
    DEL(["Delete Confirm<br/>pop-up"])
    PRE["Model Preview<br/>/models/:id/preview"]
    STA["Start Order<br/>/order/new"]
    CFG["Order Configuration<br/>/models/:id/order"]
    QPN(["Quote Panel<br/>embedded sidebar"])
    CHK(["Checkout &amp; Deposit QR<br/>pop-up"])
    ORD["My Orders<br/>/orders"]
    ODD(["Order Details<br/>drawer · timeline / payment / reprint"])
    RPP(["Reprint Request<br/>panel"])
    DCD["Date Change Decision<br/>/date-change/:token"]
    ACC["Account &amp; Addresses<br/>/account"]
    ADR(["Add / Edit Address<br/>pop-up"])
    DRQ["Design Request<br/>/design-request"]

    MOD -->|"Thêm model"| UPL
    UPL -.->|"lưu + hash"| MOD
    MOD -->|"Xóa"| DEL
    DEL -.-> MOD
    MOD -->|"Xem 3D"| PRE
    MOD -->|"Đặt in"| CFG
    PRE -->|"Đặt in"| CFG
    STA -->|"upload mới"| UPL
    STA -->|"chọn model"| CFG
    DRQ -->|"deliverable STL"| CFG

    CFG --> QPN
    QPN --> CHK
    CHK --> ORD
    ORD --> ODD
    ODD --> RPP
    ODD --> DCD
    ACC --> ADR

    classDef popup fill:#fef3c7,stroke:#d97706,stroke-width:2px
    class UPL,DEL,QPN,CHK,ODD,RPP,ADR popup
```

**Ghi chú:** `Order Details` là **Drawer** (trượt từ phải) chứ không phải route riêng — trong sơ đồ dùng
ký hiệu oval + ghi rõ "drawer". Nội dung drawer gồm 3 khối: timeline, thanh toán cọc/còn lại, yêu cầu in lại.

---

## Diagram 4 — Lab Portal (M02), đầy đủ

```mermaid
flowchart TD
    LREG["Lab Registration<br/>/lab/register"]
    LMAC["Machine Registry<br/>/lab/machines"]
    LMAI(["Maintenance Schedule<br/>pop-up"])
    LINV["Inventory<br/>/lab/:labId/inventory"]
    LSET["Lab Settings<br/>/lab/settings"]

    LQU["Lab Queue<br/>/lab/queue"]
    LDEC(["Decline Job<br/>pop-up · reason bắt buộc"])
    LCOM(["Complete Job<br/>pop-up · actuals + proof"])
    LERR(["Report Incident<br/>pop-up · ảnh"])
    LGANT["Timeline &amp; Score<br/>/lab/timeline · /lab/score"]
    LSB["Shipment Batch Planner<br/>/lab/:labId/shipments"]

    QCPR["QC Proof Review<br/>/staff/qc-proofs"]
    HUB["Hub Shipments<br/>/hub/shipments"]
    ENG{{"Rescheduler"}}

    LREG --> LMAC
    LMAC --> LMAI
    LMAC --> LINV
    LINV --> LSET
    LREG -.->|"declared capacity"| LQU

    LQU --> LDEC
    LQU --> LCOM
    LQU --> LERR
    LQU --> LGANT
    LDEC -.-> ENG
    LERR -.-> ENG
    LCOM -.->|"self-QC proof"| QCPR
    QCPR -.->|"rejected + reason"| LQU
    QCPR -.->|"approved"| LSB
    LSB ==> HUB

    classDef popup fill:#fef3c7,stroke:#d97706,stroke-width:2px
    classDef async fill:#eef2ff,stroke:#4f46e5
    classDef cross fill:#f0fdf4,stroke:#16a34a
    class LMAI,LDEC,LCOM,LERR popup
    class ENG async
    class QCPR,HUB cross
```

---

## Diagram 5 — Hub Console (M03), đầy đủ

```mermaid
flowchart TD
    HSP["Hub Shipments<br/>/hub/shipments"]
    HRC(["Reconcile Batch<br/>pop-up · RECEIVED / MISSING / DAMAGED"])
    HQC["QC Console<br/>/hub/qc"]
    HIN(["Inspection Record<br/>pop-up · checklist + ảnh"])
    HCO["Complete Orders<br/>/hub/complete-orders"]
    HWB(["Record Waybill<br/>pop-up"])
    HMG(["Merge Shipments<br/>pop-up · 1 kiện · 1 vận đơn"])
    HDP(["Confirm Delivery<br/>pop-up"])
    INC["Incident<br/>MISSING / DAMAGED"]
    ENG{{"Reprint Job<br/>URGENT"}}
    DONE["Delivered<br/>guarantee window opens"]

    HSP --> HRC
    HRC -->|"khớp"| HQC
    HRC -->|"lệch"| INC
    INC -.-> ENG
    HQC --> HIN
    HIN -->|"PASS"| HCO
    HIN -->|"FAIL — lỗi lab"| ENG
    HIN -->|"FAIL — lỗi vận chuyển"| INC
    HCO --> HWB
    HCO --> HMG
    HWB --> HDP
    HDP --> DONE

    classDef popup fill:#fef3c7,stroke:#d97706,stroke-width:2px
    classDef async fill:#eef2ff,stroke:#4f46e5
    classDef state fill:#f1f5f9,stroke:#94a3b8,stroke-dasharray: 5 5
    class HRC,HIN,HWB,HMG,HDP popup
    class ENG async
    class INC,DONE state
```

---

## Diagram 6 — Operations Console (M05), đầy đủ

```mermaid
flowchart TD
    QREV["Quote Review<br/>/quote-reviews"]
    QCPR["QC Proof Review<br/>/staff/qc-proofs"]
    QCPD(["QC Proof Detail<br/>pop-up"])
    QREJ(["Reject Proof<br/>pop-up · lý do"])
    SCH["Scheduling Board<br/>/scheduling"]
    REA(["Reassign &amp; Impact Preview<br/>pop-up 2 bước"])
    DEC["Decision Trace<br/>/ops/decisions"]
    ESC["Escalation Queue<br/>/ops/escalations"]
    OLAB["Ops Labs<br/>/ops/labs"]
    LABD(["Add Lab / Capacity Cap / Reject<br/>pop-up"])
    SLA["SLA Report<br/>/ops/sla"]
    PAY["Payments<br/>/ops/payments"]
    MPD(["Mark Paid Confirm<br/>pop-up"])
    REF["Refunds<br/>/ops/refunds"]
    RFD(["Process Refund<br/>pop-up"])
    DREQ["Design Requests<br/>/ops/design-requests"]

    QCPR --> QCPD
    QCPD --> QREJ
    QREJ -.->|"trả job về lab"| LQU2["Lab Queue<br/>(Lab)"]
    QCPD -.->|"approve"| LSB2["Shipment Batch Planner<br/>(Lab)"]

    SCH --> REA
    REA --> SCH
    SCH --> DEC
    ESC --> SCH
    OLAB --> LABD
    PAY --> MPD
    REF --> RFD

    QREV -.->|"quote PUBLISHED"| ORD["My Orders<br/>(Customer)"]
    DREQ -.->|"deliverable"| DRQ2["Design Request<br/>(Customer)"]

    classDef popup fill:#fef3c7,stroke:#d97706,stroke-width:2px
    classDef cross fill:#f0fdf4,stroke:#16a34a,stroke-dasharray: 5 5
    class QCPD,QREJ,REA,LABD,MPD,RFD popup
    class ORD,DRQ2,LQU2,LSB2 cross
```

---

## Diagram 7 — Administration (M06), có màn nhiều tab

```mermaid
flowchart TD
    AOV["Admin Overview<br/>/admin"]
    AUSR["Users &amp; Roles<br/>/admin/users"]
    AUD(["Create User<br/>pop-up"])
    ACAT[["Shared Catalog<br/>/admin/catalogs<br/>tabs: Material | Colour | Technology<br/>Quality Grade | Post-processing"]]
    ACATD(["Add / Edit Catalogue Item<br/>pop-up"])
    ACCFG["Commercial Config<br/>/admin/commercial-configurations"]
    ACCD(["New Version<br/>pop-up"])
    ASCFG["Scoring Config<br/>/admin/scoring-configurations"]
    ASCD(["Activate Weights<br/>pop-up · tổng = 1.0"])
    AQC[["QC Knowledge<br/>/admin/qc-knowledge<br/>tabs: Checklists | Defect taxonomy"]]
    AQCD(["Checklist Version / Defect Item<br/>pop-up"])
    ALAB["Lab Approval<br/>/admin/labs"]
    ALBD(["Approve / Reject Lab<br/>pop-up"])
    AAL["Audit Log<br/>/admin/audit-logs"]
    AALD(["Audit Record Detail<br/>pop-up · before / after"])
    JOBS["Background Job Dashboard<br/>/jobs"]

    AOV --> AUSR
    AOV --> ACAT
    AOV --> ACCFG
    AOV --> ASCFG
    AOV --> AQC
    AOV --> ALAB
    AOV --> AAL
    AOV -.-> JOBS

    AUSR --> AUD
    ACAT --> ACATD
    ACCFG --> ACCD
    ASCFG --> ASCD
    AQC --> AQCD
    ALAB --> ALBD
    AAL --> AALD

    ACCD -.->|"pricing params"| ENG{{"Pricing Engine"}}
    ASCD -.->|"weights"| ASG{{"Assignment Engine"}}
    ACATD -.->|"catalogues"| ENG
    AQCD -.->|"checklist version"| HQC["QC Console<br/>(Hub)"]
    ALBD -.->|"ACTIVE"| LQU["Lab Queue<br/>(Lab)"]

    classDef popup fill:#fef3c7,stroke:#d97706,stroke-width:2px
    classDef tabs fill:#ede9fe,stroke:#7c3aed,stroke-width:2px
    classDef async fill:#eef2ff,stroke:#4f46e5
    classDef cross fill:#f0fdf4,stroke:#16a34a
    class AUD,ACATD,ACCD,ASCD,AQCD,ALBD,AALD popup
    class ACAT,AQC tabs
    class ENG,ASG async
    class HQC,LQU cross
```

---

## Đối chiếu route ↔ node (tự kiểm tra trước khi nộp)

| Diagram | Screen phẳng | Màn nhiều tab | Pop-up / drawer | Route |
|---|---|---|---|---|
| 2 | 6 | — | — | `/`, `/login`, `/register`, `/verify-email`, `/forgot-password`, `/reset-password` |
| 3 | 9 | — | 7 | `/models`, `/library`, `/models/:id/preview`, `/order/new`, `/models/:id/order`, `/orders`, `/date-change/:token`, `/account`, `/design-request` |
| 4 | 7 | — | 4 | `/lab/queue`, `/lab/register`, `/lab/machines`, `/lab/timeline`, `/lab/score`, `/lab/:labId/inventory`, `/lab/:labId/shipments`, `/lab/settings` |
| 5 | 3 | — | 6 | `/hub/qc`, `/hub/shipments`, `/hub/complete-orders` |
| 6 | 9 | — | 6 | `/quote-reviews`, `/staff/qc-proofs`, `/scheduling`, `/ops/decisions`, `/ops/escalations`, `/ops/labs`, `/ops/sla`, `/ops/payments`, `/ops/refunds`, `/ops/design-requests` |
| 7 | 9 | **2** | 7 | `/admin`, `/admin/users`, `/admin/catalogs`, `/admin/commercial-configurations`, `/admin/scoring-configurations`, `/admin/qc-knowledge`, `/admin/labs`, `/admin/audit-logs`, `/jobs` |
| — | 2 | — | — | `/forbidden`, `*` (404) |

**45 route · 2 màn nhiều tab · 30 pop-up/drawer** — tất cả đều tồn tại trong code,
không có node hư cấu.

> **Còn thiếu (chưa code):** `/samples`, `/pricing`, `/network` — 3 màn public đang chờ duyệt
> FR-CUST-014..016 (`workbook/07b_De-xuat-FR-Customer-Portal.md`). Nếu GVHD duyệt thì thêm 3 node
> nét đứt vào Diagram 1 với nhãn "planned".

---

## Cách export & chèn vào Word

1. Dán **Diagram 1** vào [mermaid.live](https://mermaid.live) → Actions → *PNG (300 dpi, transparent)*.
   Đặt làm **Hình 3.1 – System Screens Flow**. Sơ đồ đã ở bố cục dọc (`flowchart TB`) cho khổ A4 portrait;
   nếu muốn trải ngang A3 thì đổi thành `flowchart LR`.
2. Export Diagram 2–7 riêng, đặt ở phụ lục hoặc thay thế Hình 3.1 nếu GVHD yêu cầu tách theo portal.
3. Dưới mỗi hình thêm chú giải ký hiệu (copy bảng "Ký hiệu" ở đầu file) — template yêu cầu rõ
   "oval notation for pop-up", nên phải có legend để người đọc hiểu.
4. Ảnh chụp màn hình thật (nếu có) nên đặt cạnh từng diagram để đối chiếu, không thay thế diagram.
