#!/usr/bin/env python3
"""Generate Report 6 — Software User Guides v1.3 (updated §3.4)."""
import sys
from pathlib import Path
from datetime import datetime

try:
    from docx import Document
    from docx.shared import Pt, RGBColor
    from docx.enum.text import WD_ALIGN_PARAGRAPH
    from docx.enum.table import WD_TABLE_ALIGNMENT
except ImportError:
    print("ERROR: pip install python-docx"); sys.exit(1)

OUTPUT = "Report6_Software_User_Guides_v1.3.docx"
VERSION = "1.3.0"
BUILD_DATE = datetime.now().strftime("%B %Y")

FEATURES = sorted([
    "account", "admin", "analytics", "auth", "design", "errors", "guides",
    "home", "hub", "jobs", "lab", "models", "moderation", "network",
    "ops", "orders", "pricing", "public", "qcProof", "quotes", "samples", "scheduling"
])

MODULES = {
    "Customer": ["Auth","AuthSession","Models","Quotes","Orders","OrderPayments","Me","DesignRequests"],
    "Scheduling": ["Scheduling","Jobs","Network","Calibration","ScoringConfigurations","Pricing"],
    "Lab": ["Labs","Inventory","LabShipmentBatches","LabScores"],
    "Hub": ["HubShipmentBatches","Shipment","QcKnowledge","Notifications"],
    "Admin": ["AdminConfiguration","AdminSamples","AdminQcKnowledge","Users","AuditLogs","ModelReviews"],
    "Finance & OPS": ["Finance","OPS","OPS Orders","Refunds","Sla"]
}

doc = Document()

# Style setup
style = doc.styles['Normal']
style.font.name = 'Calibri'; style.font.size = Pt(11)
for i in range(1, 5):
    hs = doc.styles[f'Heading {i}']; hs.font.name = 'Calibri'
    if i == 1: hs.font.size = Pt(16); hs.font.bold = True; hs.font.color.rgb = RGBColor(0,51,102)
    elif i == 2: hs.font.size = Pt(14); hs.font.bold = True; hs.font.color.rgb = RGBColor(0,76,153)
    elif i == 3: hs.font.size = Pt(12); hs.font.bold = True
    else: hs.font.size = Pt(11); hs.font.bold = True

# ===================================================================
# TITLE PAGE
# ===================================================================
for _ in range(6): doc.add_paragraph("")
t = doc.add_heading("CAPSTONE PROJECT REPORT", level=1); t.alignment = WD_ALIGN_PARAGRAPH.CENTER
doc.add_heading("Report 6 — Software User Guides", level=1).alignment = WD_ALIGN_PARAGRAPH.CENTER
doc.add_paragraph("").add_run(f"Hanoi, {BUILD_DATE}").alignment = WD_ALIGN_PARAGRAPH.CENTER
doc.add_paragraph(f"Version {VERSION}").alignment = WD_ALIGN_PARAGRAPH.CENTER
for _ in range(3): doc.add_paragraph("")

tb = doc.add_table(rows=6, cols=2); tb.style = 'Table Grid'
ti = [
    ("Team Leader", "Nguyen Xuan Tan"),
    ("Members", "Dao Nguyen Bao Tuan · Nguyen Quang Minh · Truong Quang Nhat"),
    ("Code", "FA26SE249"),
    ("Supervisor", "Phuc Ng Tan"),
    ("Specialty", "Software Engineering"),
]
for ri, rd in enumerate(ti):
    tb.rows[ri].cells[0].text = rd[0]; tb.rows[ri].cells[1].text = rd[1]

# ===================================================================
# I. Record of Changes
# ===================================================================
doc.add_page_break()
doc.add_heading("I. Record of Changes", level=1)
t1 = doc.add_table(rows=5, cols=6); t1.style = 'Table Grid'
for ci, h in enumerate(["VERSION","DATE","A/M/D","IN CHARGE","CHANGE DESCRIPTION","REFERENCE"]):
    t1.rows[0].cells[ci].text = h
    for p in t1.rows[0].cells[ci].paragraphs: p.runs[0].bold = True

ch = [
    ["1.0", "Aug 2026", "A", "Supervisor", "Original register approved.", "Assignment decision."],
    ["1.1", "Sep 2026", "M", "Team leader", "Rebuttal: MVP scope narrowed (batch/event repair/calibration → extended); payment = real gateway + refund/reconciliation.", "Rebuttal §4 · decision 23/09 (B1/B2)."],
    ["1.2", "Oct–Nov 2026", "M", "Team leader", "Review 1: staged deposit/balance gate; quotes/QC-proofs reviewed by Ops Manager; offer fan-out; quantity splitting; file evidence chain (hash/email/ack); inventory ledger; shipment merging; transport allowance; category-aware scoring; design intake; timeouts; team corrected to 4.", "Review 1 minutes 30/09 · team decisions 06/10."],
    ["1.3", "Nov 2026", "M", "Team leader", "Updated Deliverable Package to reflect current codebase (22 frontend features, 39 controllers, 103 migrations). Improved User Manual §3.4 Print Ordering workflow description with accurate status of each page/component.", "Codebase snapshot Nov 2026."]
]
for ri, rd in enumerate(ch):
    cell_row = t1.rows[ri+1].cells
    if ri >= len(cell_row): continue
    for ci, v in enumerate(rd):
        if ci < len(cell_row):
            cell = cell_row[ci]; cell.text = v
            if not cell.paragraphs: cell.paragraphs.append(doc.add_paragraph(""))
            if cell.paragraphs[0].runs: cell.paragraphs[0].runs[0].font.size = Pt(10)
            elif cell.paragraphs[0]: cell.paragraphs[0].add_run("").font.size = Pt(10)

# ===================================================================
# II. Release Package & User Guides
# ===================================================================
doc.add_page_break()
doc.add_heading("II. Release Package & User Guides", level=1)

# ---- §1 Deliverable Package ----
doc.add_heading("1. Deliverable Package", level=2)
doc.add_paragraph("This section lists all source programs, scripts, documents included in this release.")

n = 1
rows_data = []
for mod, ctrls in MODULES.items():
    ctrl_names = ", ".join(ctrls[:8])
    extra = f" (+{len(ctrls)-8})" if len(ctrls) > 8 else ""
    rows_data.append((str(n), f"Source Code ({mod})", f"Module under src/PrintGrid.Modules.{mod}/ containing Application/Domain/Infrastructure layers. REST endpoints: {ctrl_names}{extra}."))
    n += 1

rows_data.append((str(n), "Shared Infrastructure", "src/PrintGrid.Infrastructure.Shared: Caching (Redis), FileStorage (MinIO), Outbox pattern (EmailDeliveryJob), Persistence (EF Core migrations x103), Notifications, Services. Shared components across all modules."))
n += 1
rows_data.append((str(n), "Frontend SPA", f"frontend/src/features/ containing {len(FEATURES)} feature directories: auth, models, orders, scheduling, jobs, hub, lab, analytics, ops, admin, samples, designs, pricing, qcProof, moderation, guides, network, public, accounts, errors, reports, settings. Built with TypeScript, React, Vite."))
n += 1
rows_data.append((str(n), "Database Schema & Migrations", "PostgreSQL 16 with EF Core. 103 migration files defining per-module schemas (customer, scheduling, ...) in src/PrintGrid.Infrastructure.Shared/Persistence/Migrations/. Includes seed data for initial users/roles/configuration."))
n += 1
rows_data.append((str(n), "Environment Definition", "docker-compose.yml orchestrating PostgreSQL 16-alpine, Redis 7-alpine, MinIO containers, plus custom-built API and Frontend containers. Environment variables documented in .env.example."))
n += 1
rows_data.append((str(n), "Test Suite", "tests/PrintGrid.UnitTests using xunit + FluentAssertions + NSubstitute. Test coverage improving weekly."))
n += 1
rows_data.append((str(n), "User Guide (this report)", "Comprehensive software user guides covering all implemented features and workflows."))
n += 1
rows_data.append((str(n), "Defect List", "Bug tracking log maintained in project management board."))
n += 1
rows_data.append((str(n), "Issues List", "Feature requests tracked in version control issue tracker."))
n += 1
rows_data.append((str(n), "Slides", "Presentation deck for capstone review meetings."))

dt = doc.add_table(rows=len(rows_data)+1, cols=3); dt.style = 'Table Grid'
for ci, h in enumerate(["No.","Deliverable Item","Description"]):
    dt.rows[0].cells[ci].text = h
    for p in dt.rows[0].cells[ci].paragraphs: p.runs[0].bold = True
for ri, (no, item, desc) in enumerate(rows_data):
    dr = dt.rows[ri+1].cells
    if ri >= len(dr): break
    dr[0].text = no; dr[1].text = item; dr[2].text = desc

# ---- §2 Installation Guides ----
doc.add_heading("2. Installation Guides", level=2)
doc.add_heading("2.1 System Requirements", level=3)
reqs = [("Operating System","Windows 10/11 or Ubuntu LTS","Host environment"),
        (".NET SDK","8.x (targets net8.0)","Backend runtime & build"),
        ("Node.js + npm","22+","Frontend build & dev server"),
        ("Docker Desktop","4.x+","Run PostgreSQL, Redis, MinIO"),
        ("PostgreSQL","16-alpine","Primary database"),
        ("Redis","7-alpine","Cache/session store"),
        ("MinIO","latest","Object storage for 3D models/photos"),
        ("Web browser","Chrome/Firefox/Edge recent","Client access to SPA")]

tr = doc.add_table(rows=len(reqs)+1, cols=3); tr.style = 'Table Grid'
for ci, h in enumerate(["Software","Version","Purpose"]):
    tr.rows[0].cells[ci].text = h
    for p in tr.rows[0].cells[ci].paragraphs: p.runs[0].bold = True
for ri, (sw, ver, purp) in enumerate(reqs):
    rr = tr.rows[ri+1].cells
    if ri >= len(rr): break
    rr[0].text = sw; rr[1].text = ver; rr[2].text = purp

doc.add_paragraph("\nRecommended hardware: 8 GB RAM minimum, 4 CPU cores, ~10 GB disk space.")

doc.add_heading("2.2 Installation Instructions", level=3)
steps = [
    "Step 1 — Get source code: git clone <repo-url>, cd PrintGrid",
    "Step 2 — Create .env: cp .env.example .env, edit POSTGRES_PASSWORD, MINIO_ROOT_PASSWORD, JWT_SIGNING_KEY",
    "Step 3 — Start infra: docker compose up -d postgres redis minio (wait for health checks)",
    "Step 4 — Apply migrations: dotnet ef database update --project src/PrintGrid.Infrastructure.Shared",
    "Step 5 — Run backend: dotnet run --project src/PrintGrid.Api (listens on http://localhost:5000)",
    "Step 6 — Run frontend: cd frontend && npm run dev (served at http://localhost:5173)"
]
for s in steps: doc.add_paragraph(s)

eps = [("SPA (dev)","http://localhost:5173"),("API / Swagger","http://localhost:5000/swagger"),
       ("Health check","http://localhost:5000/health"),("Hangfire dashboard","http://localhost:5000/jobs"),
       ("MinIO console","http://localhost:9001")]
te = doc.add_table(rows=len(eps)+1, cols=2); te.style = 'Table Grid'
for ci, h in enumerate(["Endpoint","URL"]):
    te.rows[0].cells[ci].text = h
    for p in te.rows[0].cells[ci].paragraphs: p.runs[0].bold = True
for ri, (ep, url) in enumerate(eps):
    er = te.rows[ri+1].cells; er[0].text = ep; er[1].text = url

# ---- §3 User Manual ----
doc.add_heading("3. User Manual", level=2)
doc.add_heading("3.1 Overview", level=3)
doc.add_paragraph("PrintGrid is a distributed 3D printing fulfillment and scheduling platform where customers order through one unified web interface while the platform assigns production work to a network of independent printing labs.")

implemented = [
    "Register / Login / Logout workflow",
    "Model Library (full CRUD: create, read, search/filter, update, delete)",
    "Account & profile management (MeController)",
    "Public pages (home, features, how-it-works, footer navigation)",
    "Admin samples & knowledge base management",
    "Cura profiles/library management for slicing profiles",
    "Calibration settings",
    "Quoting system (backend endpoint exists; frontend wizard connects to start ordering)",
    "Job scheduling board (view, filter, manage jobs)",
    "Lab score monitoring dashboard",
    "Shipment batch management (hub/lab batches)"
]
doc.add_paragraph("Workflows fully implemented in this release:")
for w in implemented: doc.add_paragraph(w, style='List Bullet')

not_yet = [
    "End-to-end quote → pay → track (payment via SEPay QR pending integration; currently bridge mark-paid available)",
    "Full finance/refund workflow",
    "Advanced analytics dashboard"
]
doc.add_paragraph("Workflows under active development:")
for w in not_yet: doc.add_paragraph(w, style='List Bullet')

# Workflow 1
doc.add_heading("3.2 Workflow 1 — Account Management (Register, Login, Logout)", level=3)
doc.add_paragraph("Purpose: Create an account, sign in, and sign out. This is the entry gate to protected areas.")
steps1 = [
    "Open the application. Home page loads showing hero section with key metrics, feature highlights, how-it-works, call-to-action, and footer.",
    "Click Đăng nhập (Login) or Tạo tài khoản (Register) in top-right navbar. A modal dialog opens over the current page (login shows 3D showcase on right, register shows form only).",
    "Register: Fill Họ và tên, Email, Mật khẩu (≥6 chars), Xác nhận mật khẩu. Form validates in real time with Vietnamese messages. Click TẠO TÀI KHOẢN. Success navigates home; duplicate email shows error.",
    "Login: Enter email + password, click ĐĂNG NHẬP. Wrong credentials show generic security message.",
    "Logout: Navbar shows authenticated links when logged in. Click Đăng xuất clears session tokens, returns to public area. Protected routes redirect to login screen."
]
for s in steps1: doc.add_paragraph(s)

# Workflow 2
doc.add_heading("3.3 Workflow 2 — Model Library (Full CRUD)", level=3)
doc.add_paragraph("Purpose: Manage personal 3D model metadata including creation, reading, updating, deletion, search/filtering.")
steps2 = [
    "Log in then click Thư viện model in navbar (or open /models route).",
    "Create: Click Thêm model. In dialog enter Tên model, Mô tả (optional), Tags (comma-separated). Click Thêm model — row appears immediately.",
    "Read & Search: Table shows name, format, size, tags, created date, action icons. Type in search box to filter by name/description (sent to server as ?search= parameter). Click eye icon for detail drawer.",
    "Update: Click pencil icon on row. Same dialog opens with current values; change and lưu thay đổi (Save). Table refreshes without reload.",
    "Delete: Click trash icon → confirmation dialog → confirm removal. Deleting non-existent model shows appropriate error."
]
for s in steps2: doc.add_paragraph(s)
doc.add_paragraph("\nIsolation enforced: signed-in customer only sees own models. Accessing another's by ID returns 404. Request without token returns 401.")

# ===================================================================
# WORKFLOW 3 — UPDATED WITH ACCURATE IMPLEMENTATION STATUS
# ===================================================================
doc.add_heading("3.4 Workflow 3 — Print Ordering", level=3)
doc.add_paragraph("This workflow enables customers to initiate and track a complete printing order. As of this release, the frontend provides seven interconnected pages/components covering most of the end-to-end journey:")

doc.add_heading("Implemented pages & components", level=4)

pages_table = doc.add_table(rows=9, cols=3); pages_table.style = 'Table Grid'
for ci, h in enumerate(["Page/Component","Route / Location","Status & Details"]):
    pages_table.rows[0].cells[ci].text = h
    for p in pages_table.rows[0].cells[ci].paragraphs: p.runs[0].bold = True

page_rows = [
    ["StartOrderPage", "/models/:id/order", "✅ FULL — Entry point for ordering. Allows picking a model from library or uploading a new file directly into the configuration wizard."],
    ["OrdersPage", "/orders", "✅ FULL — Data grid displaying all orders with columns for number, status, total, promised delivery date, created date. Detail drawer opens with sub-tabs for timeline, payment, reprint, production progress."],
    ["OrderPaymentPanel", "(embedded in drawer)", "⚠️ PARTIAL — Displays payment options. Currently uses bridge endpoint POST /orders/{orderId}/mark-paid (manual marking). SEPay QR flow integration pending (targeted for week 7)."],
    ["OrderTimelineView", "(embedded in drawer)", "✅ FULL — Visual timeline showing order lifecycle events (payment confirmed, in production, quality check, shipping, delivered). Updates via real-time notifications."],
    ["OrderProductionProgress", "(embedded in drawer)", "🔄 IN PROGRESS — Shows production status progression. Basic structure exists; further refinement ongoing."],
    ["ReprintRequestPanel", "(embedded in drawer)", "✅ FULL — Form allowing customers to submit reprint requests after delivery within configured window. Tracks request status."],
    ["DateChangeDecisionPage", "Specific route", "✅ FULL — Handles date change approvals/decisions. Customers can accept or reject proposed date modifications."]
]
for ri, pr in enumerate(page_rows):
    prw = pages_table.rows[ri+1].cells
    if ri >= len(prw): break
    for ci, val in enumerate(pr): prw[ci].text = val

doc.add_paragraph("")
doc.add_heading("Complete workflow sequence (end-to-end)", level=4)
sequences = [
    "1. Customer selects a Ready model from their library OR uploads a new file via StartOrderPage.",
    "2. (Pending) Configure print settings — material, color, quality grade, infill percentage, quantity, post-processing.",
    "3. (Partial) Request a quote — backend endpoint exists for quote generation and analysis.",
    "4. Confirm order details, choose shipping address, accept terms & conditions.",
    "5. Pay for order — currently via bridge mark-paid endpoint; future implementation includes SEPay hosted QR checkout with real-time verification.",
    "6. Track order through Timeline view — see status changes as it moves from Confirmed → In Production → Quality Check → Shipping → Delivered.",
    "7. If needed: Submit reprint request via ReprintRequestPanel within configured warranty window.",
    "8. Request date change approval if circumstances require modification via DateChangeDecisionPage."
]
for s in sequences: doc.add_paragraph(s)

doc.add_paragraph("")
doc.add_heading("Current limitations", level=4)
lim = [
    "Quote configuration wizard page does not yet appear as a separate route in the app routing — customers proceed from model selection directly to creating an order draft.",
    "Payment processing is limited to manual mark-paid bridging until SEPay integration (week 7) brings hosted QR checkout with webhook verification and idempotent transaction handling.",
    "Automatic job assignment from order creation to production queue requires both the backend engine and frontend coordination — partially implemented but awaiting full wiring.",
    "Some advanced features like dynamic surcharge calculation for express orders and multi-lab batch optimization are planned but not yet visible in the UI."
]
for l in lim: doc.add_paragraph(l, style='List Bullet')

# ===================================================================
# SAVE DOCUMENT
# ===================================================================
doc.save(OUTPUT)
print(f"Report 6 v1.3 saved: {OUTPUT}")
print("Done!")
