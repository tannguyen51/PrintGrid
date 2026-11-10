$src = 'D:\Downloads\SWP490_R2_BM04_Review2-Document';
$out = 'E:\3D PRINT\temp-bm03\output';
$word = New-Object -ComObject Word.Application; $word.Visible = $false;
$doc = $word.Documents.Open("$src\SWP490_R2_BM02_Self_Review_Report_1.docx");
function SetCell($table, $row, $col, $text) { $table.Cell($row,$col).Range.Text = $text; }

# Table 1 - Run Information
$t = $doc.Tables.Item(1);
SetCell $t 1 2 'FA26SE249';
SetCell $t 2 2 'PrintGrid: Development of a Distributed 3D Printing Fulfillment and Scheduling Platform for a Network of Independent Printing Labs';
SetCell $t 3 2 'Nguyen Xuan Tan (SE181599, Leader) · Dau Nguyen Bao Tuan (SE185029) · Nguyen Quang Minh (SE181684) · Truong Quang Nhat (SE181682)';
SetCell $t 4 2 'Nguyen Tan Phuc (phucnt40@fpt.edu.vn)';
SetCell $t 5 2 'Claude Code CLI (Anthropic) — ds/deepseek-flash';
SetCell $t 6 2 '02/11/2026';
SetCell $t 7 2 'review-2 · commit ed231b7 (tag to be pushed)';
SetCell $t 8 2 'v1.2 FULL (proposed final) + FR-CUST-014..016 proposal (07b, 06/10/2026)';
SetCell $t 9 2 '1.0';
SetCell $t 10 2 '02/11/2026';

# Table 3 - Findings and Team Response
$t = $doc.Tables.Item(3);
$rows = @(
    'SR-01|Scope|SRS FR matrix §0; registration v1.2 §3.2c; frontend routes|Public sample library, pricing page and network page are implemented, but FR-CUST-014..016 are still marked Proposed in 07b and are absent from the approved SRS matrix and the registration form.|High|07b_De-xuat-FR-Customer-Portal.md §3; src/PrintGrid.Api/Controllers/SamplesController.cs:19; frontend PublicNavbar.tsx:16-17; routes /samples, /pricing, /network|Planned — Nguyen Xuan Tan, 08/11/2026 (submit FR-CUST-014..016 with BM03)',
    'SR-02|Data|Report 3 §Entities; Report 4 §2.3|SampleModel is a persisted entity (admin.sample_models) used by admin and public APIs, but it is missing from the 28-entity Logical ERD and from the table descriptions.|High|printgrid-schema.sql: CREATE TABLE admin.sample_models; SampleModel.cs; Report3_ERD.md entity table rows 1-28|Planned — Nguyen Xuan Tan, 08/11/2026',
    'SR-03|Requirements / Design vs code|FR-CUST-012; OrderPaymentsController|FR-CUST-012 requires gateway webhooks and idempotent transaction ids for deposit and balance; the code exposes only an Ops mark-paid action, with no gateway call, transaction id or idempotency key.|High|OrderPaymentsController.cs:44-51; MarkOrderPaidCommand; audit BR-PAY-005/006|Planned — Nguyen Xuan Tan, 20/11/2026',
    'SR-04|Requirements / Design vs code|FR-LAB-004; BR-ASSIGN-010; assignment flow|Registration and BR-ASSIGN-010 require offer fan-out to the top-k labs and first valid accept; the implementation assigns one lab directly and only that lab can accept or decline.|High|IAssignmentEngine.cs; no Offer aggregate in printgrid-schema.sql; JobsController.cs accept/decline endpoints|Planned — Nguyen Quang Minh, 20/11/2026',
    'SR-05|States|Print-Job-State-Machine.drawio; JobStatus.cs|The state machine has ReadyForShipment and a separate AwaitingInspection (Received at Hub) state that are absent from JobStatus; JobStatus.Cancelled has no state or transition in the diagram.|Medium|JobStatus.cs:3-14; Print-Job-State-Machine.drawio node and edge labels|Planned — Nguyen Quang Minh, 10/11/2026',
    'SR-06|Requirements|FR-CUST-006; QuoteItem|FR-CUST-006 and BR-LOG-004 require a shipping line with a bounded loss in the quote breakdown; QuoteItem stores only material and machine-time cost, so delivery is not quoted.|High|QuoteItem.cs fields; audit BR-LOG-004|Planned — Nguyen Quang Minh, 15/11/2026',
    'SR-07|Architecture / Security|NFR-SEC-007; BR-ACCESS-001; JobsController|NFR-SEC-007 requires access through short-lived links; GET /jobs/{id}/file streams the object from the API with a controller-side check and no expiring pre-signed URL.|High|JobsController.cs:116-175; GetPresignedUrlAsync is used only for QC photos at JobsController.cs:314|Planned — Dau Nguyen Bao Tuan, 15/11/2026',
    'SR-08|Requirements|NFR-SEC-008|NFR-SEC-008 has no measurable threshold or method (Metric: session security configuration) and the code uses 1-hour access and 7-day refresh tokens, not the stated 30-minute inactivity timeout.|Medium|10-Non-Functional-Requirements.md NFR-SEC-008; appsettings.json Jwt section|Planned — Nguyen Xuan Tan, 10/11/2026',
    'SR-09|Flows|FR-CUST-009; FR-HUB-002; notification handlers|A QC failure attributed to the customer raises CustomerFaultInspectionFailedEvent with no handler, so the promised status and reprint email is never sent; assignment and QC-failure notifications are also missing.|Medium|Job.cs:453; JobEvents.cs:60; no handler found in src; audit BR-QC-006/BR-NOTIFY-001|Planned — Nguyen Xuan Tan, 15/11/2026',
    'SR-10|Flows|FR-SCHED-007; DateChangeRequest|A date-change proposal is raised only when AttemptNumber >= 2, although the SDD requires a proposal whenever placement is infeasible; no proposal email is queued.|Medium|RepairAtRiskJobCommandHandler.cs; audit BR-SCHED-011/BR-NOTIFY-002|Planned — Nguyen Quang Minh, 15/11/2026',
    'SR-11|Architecture|Report 4 §1.2; 14-System-Architecture|Google Identity login and Gmail SMTP delivery are used in code and configuration but are absent from the architecture and component diagrams and from the technology stack.|Medium|appsettings.json Google and Smtp sections; AuthController Google endpoint; SmtpEmailService.cs; 14-System-Architecture.md §1.2/6.3|Planned — Nguyen Xuan Tan, 10/11/2026',
    'SR-12|Architecture|Report 4 §1.2; docker-compose.yml|The diagram shows an Nginx reverse proxy and Redis pub/sub, but compose has no Nginx service and code uses Redis only as IDistributedCache; no pub/sub channel exists.|Low|docker-compose.yml; RedisCacheService.cs; 14-System-Architecture.md §7.2|Kept — the diagram is the target deployment; local compose is a dev shortcut and will be labelled as such',
    'SR-13|Design vs code|BR-ASSIGN-002; ScoringWeightConfiguration|The code defaults use weights 0.35/0.25/0.20/0.10/0.10 while the SDD and BR-ASSIGN-002 state 0.30/0.25/0.20/0.15/0.10; no seed migration reconciles the active version.|Medium|assignment scorer defaults; audit BR-ASSIGN-002; ScoringWeightConfiguration.cs|Planned — Nguyen Quang Minh, 10/11/2026',
    'SR-14|Design vs code|FR-SCHED-006; backward-scheduling section|The internal due date is promised date minus a fixed 2-day hub and transit buffer, not a backward pass through packing, inspection, hub transfer and transit; stage buffers are not represented.|Medium|OrderConfirmedEventHandler.cs:30; PlacementShortfallService.cs:20; audit BR-SCHED-001|Planned — Nguyen Quang Minh, 15/11/2026',
    'SR-15|Requirements|SRS FR matrix §0; registration v1.2 §3.2c|FR-CUST-012 and FR-CUST-013 are still marked Proposed (R1) in the SRS matrix although the registration presents staged payment and design intake as committed; approval status is inconsistent.|Medium|09-Functional-Requirements.md rows FR-CUST-012/013; registration v1.2 §3.2c|Planned — Nguyen Xuan Tan, 08/11/2026'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 7; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 4 - Top 5 Findings
$t = $doc.Tables.Item(4);
$rows = @(
    '1|SR-01|Public sample, pricing and network pages exist without an approved FR or registration item.',
    '2|SR-03|Deposit and balance are an Ops mark-paid bridge; no gateway, transaction id or idempotency.',
    '3|SR-04|Direct assignment remains; offer fan-out and first-accept are not implemented.',
    '4|SR-07|Job files are streamed without an expiring pre-signed URL, so the IP window is controller-only.',
    '5|SR-06|Shipping is not a quote line, so the committed price excludes delivery.'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 3; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 5 - Summary
$t = $doc.Tables.Item(5);
SetCell $t 2 1 'High'; SetCell $t 2 2 '6'; SetCell $t 2 3 '0'; SetCell $t 2 4 '0'; SetCell $t 2 5 '6';
SetCell $t 3 1 'Medium'; SetCell $t 3 2 '8'; SetCell $t 3 3 '0'; SetCell $t 3 4 '0'; SetCell $t 3 5 '8';
SetCell $t 4 1 'Low'; SetCell $t 4 2 '1'; SetCell $t 4 3 '0'; SetCell $t 4 4 '1'; SetCell $t 4 5 '0';

$outPath = "$out\SWP490_FA26SE249_R2_SelfReview.docx";
$doc.SaveAs2($outPath);
$doc.Close($false); $word.Quit();
Write-Host "Saved $outPath";
