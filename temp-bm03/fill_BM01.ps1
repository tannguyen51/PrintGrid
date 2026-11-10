$src = 'D:\Downloads\SWP490_R2_BM04_Review2-Document';
$out = 'E:\3D PRINT\temp-bm03\output';
$word = New-Object -ComObject Word.Application; $word.Visible = $false;
$doc = $word.Documents.Open("$src\SWP490_R2_BM01_Design_Brief_1.docx");
function SetCell($table, $row, $col, $text) { $table.Cell($row,$col).Range.Text = $text; }

# Table 1 - Header
$t = $doc.Tables.Item(1);
SetCell $t 1 2 'FA26SE249';
SetCell $t 2 2 'PrintGrid: Development of a Distributed 3D Printing Fulfillment and Scheduling Platform for a Network of Independent Printing Labs';
SetCell $t 3 2 'Nguyen Xuan Tan (SE181599, Leader) · Dau Nguyen Bao Tuan (SE185029) · Nguyen Quang Minh (SE181684) · Truong Quang Nhat (SE181682)';
SetCell $t 4 2 'Nguyen Tan Phuc (phucnt40@fpt.edu.vn)';
SetCell $t 5 2 'https://github.com/tannguyen51/PrintGrid — commit ed231b7 (tag review-2 to be pushed)';
SetCell $t 6 2 '1.0';
SetCell $t 7 2 '02/11/2026';

# Table 2 - 1.1 Registration Changes
$t = $doc.Tables.Item(2);
$rows = @(
    '3.2 b/c - Payment|Full payment before production|Deposit gates production; balance gates packing; forfeit policy|Review 1 §I.4; BR-PAY-007/008',
    '3.2 b/c - Quoting|Automatic quote|Engine draft; Ops publishes within band; auto-lane; expiry at publication|Review 1 §II.4',
    '3.2 b/c - Assignment|Direct assignment to one lab|Top-k offer fan-out; first valid accept; Ops arbitration|Review 1 §II.6',
    '3.2 b/c - Scheduling|Reassign the unstarted part|Split quantity then reassign; date-change proposal; 48 h timeout|Review 1 §I.1',
    '3.2 b/c - Evidence and IP|Links and access logs|SHA-256; receipt email; hash acknowledgement; customer-owned design|Review 1 §II.12-17',
    '3.2 c - Lab and hub|Hub-only QC; manual shipment|Lab proof gate; lab->hub batches; shipment merge; shipping quote line|Review 1 §I.2/I.5/I.7/II.8',
    '3.2 c - Customer portal|No public pages|Add FR-CUST-014..016 (sample; pricing; network)|Iteration 1; 07b proposal',
    '3.2 g - Team|Five members|Four members|Roster; decision 06/10'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 4; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 3 - 1.2 Out of Scope
$t = $doc.Tables.Item(3);
$rows = @(
    'Carrier API integration and live tracking|Manual waybill only; no carrier contract',
    'Resin and SLS printing|MVP restricted to FDM so slicing, estimation and QC stay verifiable',
    'Native mobile apps, lab e-wallets, automated payouts, in-platform 3D modelling|Resource limit; payouts are computed in-system, settled by runbook'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 2; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 4 - 3. Key Design Decisions
$t = $doc.Tables.Item(4);
$rows = @(
    'D1|Modular monolith, schema per module, no cross-module cascade|Microservices; layered monolith; modular monolith|One deployable, six modules, in-process events, extraction path kept; fits four people.|Tan|NFR-MAINT-004; Report 4 §1.2',
    'D2|Database-enforced no double booking|App lock; serializable; PostgreSQL EXCLUDE|EXCLUDE on queue key and time range; 23P01 maps to 409; works at machine or workshop level.|Minh|FR-SCHED-006; NFR-REL-004',
    'D3|Draft quote with frozen version and staff gate|Lead-time table; auto-publish; draft plus review|Engine drafts from a frozen version and speculative placement; Ops publishes within a band; audit stored.|Tan|FR-CUST-006; FR-SCHED-005/010',
    'D4|Lab self-QC proof gates hub dispatch|Hub-only QC; self-QC without gate; proof plus Ops approval|Completion needs actuals and photo proof; Ops approval gates batch sealing; rejection returns to InProgress.|Nhat|FR-LAB-005; FR-HUB-001/003',
    'D5|Evidence-first file handover on object storage|File server; signed links; object storage with hash chain|MinIO plus SHA-256 at upload, receipt email, partner hash acknowledgement and access audit.|Tuan|FR-CUST-002; NFR-SEC-007'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 6; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 5 - 4. Top Risks
$t = $doc.Tables.Item(5);
$rows = @(
    'R1|Scheduling promise fails on real lab data|Missed dates; weak research result|Simulator 10/25/50 labs with faults; calibration per machine model; partner-lab pilot; MAPE alert at 25%.|Minh',
    'R2|No gateway; Ops records deposit and balance|No online checkout; demo diverges from FR-CUST-012|Gateway sandbox for both legs with idempotency and reconciliation; Ops bridge only as fallback.|Tan',
    'R3|File streamed without an expiring URL|IP leak; loss of trust|Pre-signed URL for job plus 4 h; revoke on completion; audit sample; cross-lab and expiry tests.|Tuan'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 5; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 6 - 5. Known Gaps
$t = $doc.Tables.Item(6);
$rows = @(
    'G-01|Public sample, pricing and network pages lack an approved FR and a registration item.|SRS matrix §0; registration §3.2c; SamplesController.cs:19|Submit FR-CUST-014..016 in BM03; if rejected remove the pages.|Tan|08/11/2026',
    'G-02|SampleModel and admin.sample_models are missing from the ERD and database design.|Report 3 §Entities; Report 4 §2.3; printgrid-schema.sql|Add the entity and relations; regenerate the ERD.|Tan|08/11/2026',
    'G-03|No gateway; FR-CUST-012 webhook and idempotency are not implemented.|OrderPaymentsController.cs:44-51; BR-PAY-005/006|Integrate the gateway for both legs; add transaction id and idempotency key.|Tan|20/11/2026',
    'G-04|Top-k offer fan-out and first-accept are not implemented.|FR-LAB-004; BR-ASSIGN-010; IAssignmentEngine.cs|Add Offer aggregate, fan-out, 2 h window and Ops arbitration.|Minh|20/11/2026',
    'G-05|Shipping is not a quote line.|FR-CUST-006; BR-LOG-004; QuoteItem.cs|Add shipping cost, zone rate and loss tolerance to the quote.|Minh|15/11/2026',
    'G-06|Job file is streamed; no expiring pre-signed URL.|JobsController.cs:116-175; NFR-SEC-007|Issue a pre-signed URL for job plus 4 h; revoke on completion.|Tuan|15/11/2026'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 6; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 7 - 6. Proposed Mandatory Features
$t = $doc.Tables.Item(7);
$rows = @(
    'F-01|Customer registration and authentication|Customer|Session on register, login and refresh; duplicate email 409; wrong password and unknown email 401; cross-user 404.|UT-AUTH-001..008; IT-AUTH-001..005|Done',
    'F-02|Model upload, evidence record and 3D preview|Customer|SHA-256 stored; receipt email <=1 min; STL/3MF preview; invalid file rejected with reason.|UT-MOD-001..004; IT-MOD-001..003|Done',
    'F-03|Quote draft and staff publication|Customer; Ops Manager|Draft p95 <=60 s; out-of-band edit rejected; publish audit; frozen version reproduces price.|TC-F03-01..04|In progress',
    'F-04|Order placement and staged deposit|Customer; Ops Manager|PaymentPending until deposit %; no production before Confirmed; unpaid balance blocks packing; idempotent.|TC-F04-01..04|In progress',
    'F-05|Lab registration, machines and inventory ledger|Lab Manager|Second lab per owner 409; approval needs machine and material; stock code and running total; 1-5% tolerance.|UT-LAB-001..008; TC-F05-01..03|Done',
    'F-06|Offer acceptance, production and lab self-QC|Lab Manager; Lab Operator|Accept only in 2 h window with matching hash; completion needs actuals and photo; rejected proof returns to InProgress.|TC-F06-01..04|In progress',
    'F-07|Backward scheduling and event-driven rescheduling|Engine; Ops Manager|DB rejects overlap; decline/failure repair <=30 s; quantity split before date change; proposal expires at 48 h.|TC-F07-01..04|In progress',
    'F-08|Hub receipt, inspection, reprint and shipment|Hub QC; Hub Fulfillment|Batch receipt reconciles jobs; checklist by grade; reprint after attribution; same-customer merge; waybill recorded.|TC-F08-01..04|In progress',
    'F-09|Operations monitoring, override and SLA|Ops Manager|At-risk list by slack; override needs reason; SLA export matches history; suspension blocks new work.|TC-F09-01..03|In progress',
    'F-10|Administration, configuration and audit|System Administrator|Config effective <=1 min; every mutation audited with before/after; catalogue deprecates, never deletes.|TC-F10-01..03|Done'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 6; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 8 - 7. Non-Functional Requirements
$t = $doc.Tables.Item(8);
$rows = @(
    'NFR-PERF-001|Response time|Draft quote p95|<=60 s (preliminary <=5 s)|Timestamp 100 runs in k6',
    'NFR-PERF-002|Response time|Reschedule wall-clock|<=30 s (mean <=15 s)|Simulator, 50 concurrent events',
    'NFR-REL-004|Consistency|Double-booked slots|0|50 parallel placements; EXCLUDE assertion',
    'NFR-SEC-007|Security|Unauthorized file access|0; 100% logged|Cross-lab and expiry tests; audit sample',
    'NFR-ACC-001|Accuracy|MAPE after calibration|<=25%; >=10 samples/model|Nightly calibration report vs actuals',
    'NFR-SCALE-001|Scalability|Network size|50 labs / 300 machines|Simulator run at 10/25/50 labs'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 5; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 9 - 8. Ownership Map
$t = $doc.Tables.Item(9);
$rows = @(
    'Nguyen Xuan Tan (SE181599)|Team leader; PM/BA; backend|Customer; Admin/audit; integration|WF-05 Ops exception|D1, D3; PR #auth, #audit, #payment',
    'Nguyen Quang Minh (SE181684)|Algorithm engineer|Scheduling engine; simulator; calibration|WF-02 Placement and offer|D2; PR #assignment, #simulator',
    'Truong Quang Nhat (SE181682)|Backend/QA; Lab and Hub|Lab; Hub; QC and shipment|WF-03 Production; WF-04 Hub|D4; PR #lab, #hub, #shipment',
    'Dau Nguyen Bao Tuan (SE185029)|Frontend engineer|Customer portal; Lab/Hub UI; design system|WF-01 Order to deposit|D5; PR #customer-portal, #upload, #checkout'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 5; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 10 - A1. Main Workflows
$t = $doc.Tables.Item(10);
$rows = @(
    'WF-01|Customer order to quote and deposit|Customer; Ops Manager; Engine|Customer uploads and configures|Committed price and date plus paid deposit create a Confirmed order|F-01..F-04|Quote Pending->Draft->Ready->Converted; Order PaymentPending->Confirmed|Tuan',
    'WF-02|Placement, offer and lab acceptance|Engine; Lab Manager; Ops Manager|Order Confirmed or placement requested|Capable lab accepts; job scheduled without double booking|F-06, F-07|Job Pending->Assigned->Accepted|Minh',
    'WF-03|Production, self-QC and lab-to-hub batch|Lab Operator; Ops Manager; Hub QC|Lab accepts the job|Proof-approved jobs leave in a sealed batch|F-06, F-08|Job Accepted->InProgress->AwaitingInspection; QcProof None->Pending->Approved; Batch Sealed|Nhat',
    'WF-04|Hub inspection, reprint and delivery|Hub QC; Hub Fulfillment; Customer|Batch received at hub|Inspected, consolidated and delivered order, or attributed reprint|F-08|Job AwaitingInspection->Completed/Failed->Reassigned; Order QualityCheck->Shipping->Delivered|Nhat',
    'WF-05|Ops exception and date-change decision|Ops Manager; Customer; Engine|Forecast lateness or no feasible placement|Re-planned schedule or approved or expired date change|F-07, F-09|DateChangeRequest Proposed->Approved/Rejected/Expired|Tan'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 8; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 11 - A2. Demo Scenarios
$t = $doc.Tables.Item(11);
$rows = @(
    'DS-01|WF-01|Main|Seeded customer, model and pricing version; deposit 50%|Login -> upload -> configure -> quote -> Ops publish -> deposit -> Confirmed|Quote Ready; order Confirmed; deposit once|Tuan',
    'DS-02|WF-01|Exception|Published quote past expiry|Attempt checkout|Checkout rejected; re-quote required; no order|Tuan',
    'DS-03|WF-02|Main|Confirmed order; three capable labs|Rank labs -> accept best offer with hash acknowledgement|Job Assigned->Accepted; slot reserved; no overlap|Minh',
    'DS-04|WF-02|Exception|Top lab declines or 2 h timeout|Decline; engine re-ranks|Lab excluded; job Reassigned; decision log records reason|Minh',
    'DS-05|WF-03|Main|Accepted job at lab|Start -> complete with actuals and photo -> Ops approves -> seal -> hub receives|Job AwaitingInspection->InTransit->Completed; batch Received|Nhat',
    'DS-06|WF-03|Exception|Missing or blurred proof|Ops rejects proof|Job back to InProgress; batch cannot be sealed|Nhat',
    'DS-07|WF-04|Main|Received batch passes inspection|Inspect -> consolidate -> waybill -> deliver|Order Shipping->Delivered; one waybill|Nhat',
    'DS-08|WF-04|Exception|Batch fails inspection, lab fault|Record defect and attribution -> reprint|Reprint created; cost bearer Lab; original Failed|Nhat',
    'DS-09|WF-05|Main|Job forecast late; customer contactable|Ops proposes date -> customer approves|Request Approved; schedule re-planned; customer notified|Tan',
    'DS-10|WF-05|Exception|No customer answer within 48 h|Let request expire|Status Expired; escalation created; no silent change|Tan'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 7; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 12 - A3. Functional Test Case Target
$t = $doc.Tables.Item(12);
SetCell $t 2 1 'Mandatory features'; SetCell $t 2 2 '10'; SetCell $t 2 3 '3'; SetCell $t 2 4 '30';
SetCell $t 3 1 'Main workflows (end-to-end)'; SetCell $t 3 2 '5'; SetCell $t 3 3 '1'; SetCell $t 3 4 '5';
SetCell $t 4 1 'State transitions of main entities'; SetCell $t 4 2 '18'; SetCell $t 4 3 '1'; SetCell $t 4 4 '18';
SetCell $t 5 1 'Agreed NFRs'; SetCell $t 5 2 '6'; SetCell $t 5 3 '1'; SetCell $t 5 4 '6';
SetCell $t 6 1 'Committed total'; SetCell $t 6 2 ''; SetCell $t 6 3 ''; SetCell $t 6 4 '59';

$outPath = "$out\SWP490_FA26SE249_R2_DesignBrief.docx";
$doc.SaveAs2($outPath);
$doc.Close($false); $word.Quit();
Write-Host "Saved $outPath";
