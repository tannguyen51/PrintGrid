$src = 'D:\Downloads\SWP490_R2_BM04_Review2-Document';
$out = 'E:\3D PRINT\temp-bm03\output';
$word = New-Object -ComObject Word.Application; $word.Visible = $false;
$doc = $word.Documents.Open("$src\SWP490_R2_BM04_Review2_Commitment_1.docx");
function SetCell($table, $row, $col, $text) { $table.Cell($row,$col).Range.Text = [string]$text; }

# Table 1 - Project Information
$t = $doc.Tables.Item(1);
SetCell $t 1 2 'FA26SE249';
SetCell $t 2 2 'PrintGrid: Development of a Distributed 3D Printing Fulfillment and Scheduling Platform for a Network of Independent Printing Labs';
SetCell $t 3 2 'Nguyen Xuan Tan (SE181599, Leader) · Dau Nguyen Bao Tuan (SE185029) · Nguyen Quang Minh (SE181684) · Truong Quang Nhat (SE181682)';
SetCell $t 4 2 'Nguyen Tan Phuc (phucnt40@fpt.edu.vn)';
SetCell $t 5 2 '02/11/2026';
SetCell $t 6 2 'Reviewer 1';
SetCell $t 7 2 'Reviewer 2';
SetCell $t 8 2 '1.0';
SetCell $t 9 2 '02/11/2026';

# Table 2 - Final Registration Form
$t = $doc.Tables.Item(2);
SetCell $t 1 2 'R2-Nov09';
SetCell $t 2 2 'Yes';
SetCell $t 3 2 'SWP490_FA26SE249_R2_Registration.pdf';

# Table 3 - Mandatory Features
$t = $doc.Tables.Item(3);
$rows = @(
    'F-01|Customer registration and authentication|Customer|Session on register, login and refresh; duplicate email 409; wrong password and unknown email 401; cross-user 404.|Tuan',
    'F-02|Model upload, evidence record and 3D preview|Customer|SHA-256 stored; receipt email <=1 min; STL/3MF preview; invalid file rejected with reason.|Tuan',
    'F-03|Quote draft and staff publication|Customer; Ops Manager|Draft p95 <=60 s; out-of-band edit rejected; publish audit; frozen version reproduces price.|Tuan',
    'F-04|Order placement and staged deposit|Customer; Ops Manager|PaymentPending until deposit %; no production before Confirmed; unpaid balance blocks packing; idempotent.|Tuan',
    'F-05|Lab registration, machines and inventory ledger|Lab Manager|Second lab per owner 409; approval needs machine and material; stock code and running total; 1-5% tolerance.|Nhat',
    'F-06|Offer acceptance, production and lab self-QC|Lab Manager; Lab Operator|Accept only in 2 h window with matching hash; completion needs actuals and photo; rejected proof returns to InProgress.|Minh; Nhat'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 5; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 4 - NFR
$t = $doc.Tables.Item(4);
$rows = @(
    'NFR-PERF-001|Response time|Draft quote p95 <=60 s (preliminary <=5 s)|Timestamp 100 runs in k6|Minh',
    'NFR-PERF-002|Response time|Reschedule wall-clock <=30 s (mean <=15 s)|Simulator, 50 concurrent events|Minh',
    'NFR-REL-004|Consistency|Double-booked slots = 0|50 parallel placements; EXCLUDE assertion|Minh',
    'NFR-SEC-007|Security|Unauthorized file access = 0; 100% logged|Cross-lab and expiry tests; audit sample|Tuan'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 5; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 5 - Out of Scope
$t = $doc.Tables.Item(5);
$rows = @(
    'Carrier API integration and live tracking|Manual waybill only; no carrier contract',
    'Resin and SLS printing|MVP restricted to FDM so slicing, estimation and QC stay verifiable',
    'Native mobile apps, lab e-wallets, automated payouts, in-platform 3D modelling|Resource limit; payouts are computed in-system, settled by runbook'
);
for ($i=0; $i -lt $rows.Count; $i++) { $p = $rows[$i].Split('|'); for ($c=0; $c -lt 2; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Workflow data
$wfData = @{
    1 = @{Name='Customer order to quote and deposit'; Owner='Dau Nguyen Bao Tuan'; Supporting='Nguyen Xuan Tan'; Actors='Customer; Ops Manager; Engine'; Trigger='Customer uploads and configures'; Outcome='Committed price and date plus paid deposit create a Confirmed order'; StatePath='Quote Pending->Draft->Ready->Converted; Order PaymentPending->Confirmed'; Activity='Report 4 §3.1'; Features=@('F-01','F-02','F-03','F-04'); DS=@('DS-01|Main|Seeded customer, model and pricing version; deposit 50%|Login -> upload -> configure -> quote -> Ops publish -> deposit -> Confirmed|Quote Ready; order Confirmed; deposit once','DS-02|Exception|Published quote past expiry|Attempt checkout|Checkout rejected; re-quote required; no order')}
    2 = @{Name='Placement, offer and lab acceptance'; Owner='Nguyen Quang Minh'; Supporting='Nguyen Xuan Tan'; Actors='Engine; Lab Manager; Ops Manager'; Trigger='Order Confirmed or placement requested'; Outcome='Capable lab accepts; job scheduled without double booking'; StatePath='Job Pending->Assigned->Accepted'; Activity='Report 4 §3.2'; Features=@('F-06','F-07'); DS=@('DS-03|Main|Confirmed order; three capable labs|Rank labs -> accept best offer with hash acknowledgement|Job Assigned->Accepted; slot reserved; no overlap','DS-04|Exception|Top lab declines or 2 h timeout|Decline; engine re-ranks|Lab excluded; job Reassigned; decision log records reason')}
    3 = @{Name='Production, self-QC and lab-to-hub batch'; Owner='Truong Quang Nhat'; Supporting='Nguyen Quang Minh'; Actors='Lab Operator; Ops Manager; Hub QC'; Trigger='Lab accepts the job'; Outcome='Proof-approved jobs leave in a sealed batch'; StatePath='Job Accepted->InProgress->AwaitingInspection; QcProof None->Pending->Approved; Batch Sealed'; Activity='Report 4 §3.3'; Features=@('F-06','F-08'); DS=@('DS-05|Main|Accepted job at lab|Start -> complete with actuals and photo -> Ops approves -> seal -> hub receives|Job AwaitingInspection->InTransit->Completed; batch Received','DS-06|Exception|Missing or blurred proof|Ops rejects proof|Job back to InProgress; batch cannot be sealed')}
    4 = @{Name='Hub inspection, reprint and delivery'; Owner='Truong Quang Nhat'; Supporting='Dau Nguyen Bao Tuan'; Actors='Hub QC; Hub Fulfillment; Customer'; Trigger='Batch received at hub'; Outcome='Inspected, consolidated and delivered order, or attributed reprint'; StatePath='Job AwaitingInspection->Completed/Failed->Reassigned; Order QualityCheck->Shipping->Delivered'; Activity='Report 4 §3.4'; Features=@('F-08'); DS=@('DS-07|Main|Received batch passes inspection|Inspect -> consolidate -> waybill -> deliver|Order Shipping->Delivered; one waybill','DS-08|Exception|Batch fails inspection, lab fault|Record defect and attribution -> reprint|Reprint created; cost bearer Lab; original Failed')}
    5 = @{Name='Ops exception and date-change decision'; Owner='Nguyen Xuan Tan'; Supporting='Nguyen Quang Minh'; Actors='Ops Manager; Customer; Engine'; Trigger='Forecast lateness or no feasible placement'; Outcome='Re-planned schedule or approved or expired date change'; StatePath='DateChangeRequest Proposed->Approved/Rejected/Expired'; Activity='Report 4 §3.5'; Features=@('F-07','F-09'); DS=@('DS-09|Main|Job forecast late; customer contactable|Ops proposes date -> customer approves|Request Approved; schedule re-planned; customer notified','DS-10|Exception|No customer answer within 48 h|Let request expire|Status Expired; escalation created; no silent change')}
}

# Helper for workflow block start indices
$wfStart = @(6,12,18,24,30);
foreach ($wi in 1..5) {
    $base = $wfStart[$wi-1];
    $w = $wfData[$wi];

    # Table 6 + offset 0: workflow info
    $t = $doc.Tables.Item($base);
    SetCell $t 1 2 $w.Name;
    SetCell $t 2 2 $w.Owner;
    SetCell $t 3 2 $w.Supporting;
    SetCell $t 4 2 $w.Actors;
    SetCell $t 5 2 $w.Trigger;
    SetCell $t 6 2 $w.Outcome;
    SetCell $t 7 2 $w.StatePath;
    SetCell $t 8 2 $w.Activity;

    # Table +1: Features (4 rows)
    $t = $doc.Tables.Item($base+1);
    for ($r=0; $r -lt $w.Features.Count; $r++) {
        $fid = $w.Features[$r];
        # map feature details from mandatory features table
        switch ($fid) {
            'F-01' { SetCell $t ($r+2) 1 'F-01'; SetCell $t ($r+2) 2 'Customer registration and authentication'; SetCell $t ($r+2) 3 'Session on register, login and refresh; duplicate email 409; wrong password and unknown email 401; cross-user 404.'; SetCell $t ($r+2) 4 'TC-F01-01..03'; }
            'F-02' { SetCell $t ($r+2) 1 'F-02'; SetCell $t ($r+2) 2 'Model upload, evidence record and 3D preview'; SetCell $t ($r+2) 3 'SHA-256 stored; receipt email <=1 min; STL/3MF preview; invalid file rejected with reason.'; SetCell $t ($r+2) 4 'TC-F02-01..03'; }
            'F-03' { SetCell $t ($r+2) 1 'F-03'; SetCell $t ($r+2) 2 'Quote draft and staff publication'; SetCell $t ($r+2) 3 'Draft p95 <=60 s; out-of-band edit rejected; publish audit; frozen version reproduces price.'; SetCell $t ($r+2) 4 'TC-F03-01..04'; }
            'F-04' { SetCell $t ($r+2) 1 'F-04'; SetCell $t ($r+2) 2 'Order placement and staged deposit'; SetCell $t ($r+2) 3 'PaymentPending until deposit %; no production before Confirmed; unpaid balance blocks packing; idempotent.'; SetCell $t ($r+2) 4 'TC-F04-01..04'; }
            'F-05' { SetCell $t ($r+2) 1 'F-05'; SetCell $t ($r+2) 2 'Lab registration, machines and inventory ledger'; SetCell $t ($r+2) 3 'Second lab per owner 409; approval needs machine and material; stock code and running total; 1-5% tolerance.'; SetCell $t ($r+2) 4 'TC-F05-01..03'; }
            'F-06' { SetCell $t ($r+2) 1 'F-06'; SetCell $t ($r+2) 2 'Offer acceptance, production and lab self-QC'; SetCell $t ($r+2) 3 'Accept only in 2 h window with matching hash; completion needs actuals and photo; rejected proof returns to InProgress.'; SetCell $t ($r+2) 4 'TC-F06-01..04'; }
            'F-07' { SetCell $t ($r+2) 1 'F-07'; SetCell $t ($r+2) 2 'Backward scheduling and event-driven rescheduling'; SetCell $t ($r+2) 3 'DB rejects overlap; decline/failure repair <=30 s; quantity split before date change; proposal expires at 48 h.'; SetCell $t ($r+2) 4 'TC-F07-01..04'; }
            'F-08' { SetCell $t ($r+2) 1 'F-08'; SetCell $t ($r+2) 2 'Hub receipt, inspection, reprint and shipment'; SetCell $t ($r+2) 3 'Batch receipt reconciles jobs; checklist by grade; reprint after attribution; same-customer merge; waybill recorded.'; SetCell $t ($r+2) 4 'TC-F08-01..04'; }
            'F-09' { SetCell $t ($r+2) 1 'F-09'; SetCell $t ($r+2) 2 'Operations monitoring, override and SLA'; SetCell $t ($r+2) 3 'At-risk list by slack; override needs reason; SLA export matches history; suspension blocks new work.'; SetCell $t ($r+2) 4 'TC-F09-01..03'; }
        }
    }

    # Table +2: Demo scenarios
    $t = $doc.Tables.Item($base+2);
    for ($r=0; $r -lt $w.DS.Count; $r++) {
        $p = $w.DS[$r].Split('|');
        SetCell $t ($r+2) 1 $p[0]; SetCell $t ($r+2) 2 $p[1]; SetCell $t ($r+2) 3 $p[2]; SetCell $t ($r+2) 4 $p[3]; SetCell $t ($r+2) 5 $p[4];
    }

    # Table +3: Committed test cases (fill counts)
    $t = $doc.Tables.Item($base+3);
    $ft = 3 * $w.Features.Count;
    SetCell $t 2 2 $ft; SetCell $t 2 3 'Yes'; SetCell $t 2 4 $w.Owner;
    SetCell $t 3 2 '1'; SetCell $t 3 3 'Yes'; SetCell $t 3 4 $w.Owner;
    SetCell $t 4 2 '2'; SetCell $t 4 3 'Yes'; SetCell $t 4 4 $w.Owner;
    SetCell $t 5 2 ($ft + 3); SetCell $t 5 3 ''; SetCell $t 5 4 '';

    # Table +4: Pass criteria
    $t = $doc.Tables.Item($base+4);
    SetCell $t 4 2 '2/2';
    SetCell $t 5 2 '>=90%';
}

# Members data (4 members)
$members = @(
    @{Name='Nguyen Xuan Tan (SE181599)'; ID='SE181599'; Role='Team leader; PM/BA; backend'; Workflows='WF-05'; Features='F-03, F-04, F-09'; Modules='Customer; Admin/audit; integration'; Decisions='D1, D3'; NFRs='NFR-SEC-007, NFR-SEC-008'; Cases='18'; Automated='10'; DS='DS-09, DS-10'; Fixes='G-01, G-02, G-03, SR-01, SR-08, SR-09, SR-11, SR-15'; Docs='BM01, BM02, BM04; Report 4 §1, §3.5'},
    @{Name='Dau Nguyen Bao Tuan (SE185029)'; ID='SE185029'; Role='Frontend engineer'; Workflows='WF-01'; Features='F-01, F-02, F-03, F-04'; Modules='Customer portal; Lab/Hub UI; design system'; Decisions='D5'; NFRs='NFR-PERF-001'; Cases='15'; Automated='8'; DS='DS-01, DS-02'; Fixes='G-06, SR-07'; Docs='BM01, BM04; Report 4 §5 UI/UX'},
    @{Name='Nguyen Quang Minh (SE181684)'; ID='SE181684'; Role='Algorithm engineer'; Workflows='WF-02'; Features='F-06, F-07'; Modules='Scheduling engine; simulator; calibration'; Decisions='D2'; NFRs='NFR-REL-004, NFR-PERF-002'; Cases='14'; Automated='9'; DS='DS-03, DS-04'; Fixes='G-04, G-05, SR-04, SR-05, SR-06, SR-10, SR-13, SR-14'; Docs='BM01, BM04; Report 4 §2, §3.2, §3.5'},
    @{Name='Truong Quang Nhat (SE181682)'; ID='SE181682'; Role='Backend/QA; Lab and Hub'; Workflows='WF-03, WF-04'; Features='F-05, F-06, F-08'; Modules='Lab; Hub; QC and shipment'; Decisions='D4'; NFRs='NFR-ACC-001, NFR-SCALE-001'; Cases='12'; Automated='5'; DS='DS-05, DS-06, DS-07, DS-08'; Fixes=''; Docs='BM01, BM04; Report 4 §3.3, §3.4'}
)

$memberStart = @(36,41,46,51,56);
foreach ($mi in 0..3) {
    $m = $members[$mi];
    $base = $memberStart[$mi];

    # Table +0: basic info
    $t = $doc.Tables.Item($base);
    SetCell $t 1 2 $m.Name;
    SetCell $t 2 2 $m.ID;
    SetCell $t 3 2 $m.Role;

    # Table +1: ownership
    $t = $doc.Tables.Item($base+1);
    SetCell $t 2 2 $m.Workflows;
    SetCell $t 3 2 $m.Features;
    SetCell $t 4 2 $m.Modules;
    SetCell $t 5 2 $m.Decisions;
    SetCell $t 6 2 $m.NFRs;

    # Table +2: delivery commitments
    $t = $doc.Tables.Item($base+2);
    SetCell $t 2 2 $m.Cases; SetCell $t 2 3 '15/11/2026';
    SetCell $t 3 2 $m.Automated;
    SetCell $t 4 2 $m.DS;
    SetCell $t 5 2 $m.Fixes;
    SetCell $t 6 2 $m.Docs;
}

# Table 61 - Summary by workflow
$t = $doc.Tables.Item(61);
$wfSummary = @(
    'WF-01|Tuan|F-01..F-04|2 / 0|15|8',
    'WF-02|Minh|F-06, F-07|2 / 0|14|9',
    'WF-03|Nhat|F-06, F-08|2 / 0|12|5',
    'WF-04|Nhat|F-08|2 / 0|8|3',
    'WF-05|Tan|F-07, F-09|2 / 0|10|7'
);
for ($i=0; $i -lt $wfSummary.Count; $i++) { $p = $wfSummary[$i].Split('|'); for ($c=0; $c -lt 6; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }
SetCell $t 7 1 'Total'; SetCell $t 7 5 '59'; SetCell $t 7 6 '32';

# Table 62 - Summary by member
$t = $doc.Tables.Item(62);
$memberSummary = @(
    'Tan|WF-05|F-03, F-04, F-09|18|10|DS-09, DS-10|Verified',
    'Tuan|WF-01|F-01..F-04|15|8|DS-01, DS-02|Verified',
    'Minh|WF-02|F-06, F-07|14|9|DS-03, DS-04|Verified',
    'Nhat|WF-03, WF-04|F-05, F-06, F-08|12|5|DS-05..DS-08|Verified',
    'Member 5 (not applicable)|-|0|0|0|-|N/A'
);
for ($i=0; $i -lt $memberSummary.Count; $i++) { $p = $memberSummary[$i].Split('|'); for ($c=0; $c -lt 7; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

# Table 63 - Committed numbers
$t = $doc.Tables.Item(63);
SetCell $t 1 2 '5';
SetCell $t 2 2 '10 main / 5 exception';
SetCell $t 3 2 '59';
SetCell $t 4 2 '32 (54%)';

# Table 64 - Eligibility thresholds
$t = $doc.Tables.Item(64);
SetCell $t 2 3 '5 of 5';
SetCell $t 3 3 '10 / 5 (>=80%)';
SetCell $t 4 3 '59';
SetCell $t 5 3 '>=90%';
SetCell $t 6 3 '>=50%';
SetCell $t 7 3 '0';
SetCell $t 8 3 '4 of 4';

# Table 65 - Required fixes (from BM02 planned items)
$t = $doc.Tables.Item(65);
$fixes = @(
    'RF-01|Public sample/pricing/network pages lack approved FR|WF-01|Tan|08/11/2026',
    'RF-02|Add SampleModel to ERD and DB|WF-01|Tan|08/11/2026',
    'RF-03|Gateway webhooks and idempotency for payments|WF-01|Tan|20/11/2026',
    'RF-04|Top-k offer fan-out and first-accept|WF-02|Minh|20/11/2026',
    'RF-05|Shipping quote line and delivery cost|WF-02|Minh|15/11/2026'
);
for ($i=0; $i -lt $fixes.Count; $i++) { $p = $fixes[$i].Split('|'); for ($c=0; $c -lt 5; $c++) { SetCell $t ($i+2) ($c+1) $p[$c]; } }

$outPath = "$out\SWP490_FA26SE249_R2_Commitment.docx";
$doc.SaveAs2($outPath);
$doc.Close($false); $word.Quit();
Write-Host "Saved $outPath";
