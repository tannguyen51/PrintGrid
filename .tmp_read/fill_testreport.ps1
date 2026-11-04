$ErrorActionPreference = 'Stop'
$path = 'E:\DOC DO An\2_SEP490\Report5_Test Report.xlsx'

$projectName = 'PrintGrid - Distributed 3D Printing Fulfillment & Scheduling Platform'
$projectCode = 'FA26SE249'
$creator      = 'TanNXSE181599'
$reviewer     = 'Nguyen Tan Phuc'
$issueDate    = '02/11/2026'

$features = @(
  @{
    Sheet='Auth'; Name='Authentication & Account'; Req='FR-CUST-001 - register, login, refresh, Google sign-in, forgot/reset password, session logout.';
    Cases=@(
      @('TC-AUTH-01','Register a new customer','1. POST /auth/register with a valid name, email and password.`n2. Read the response body.','201 Created; access and refresh tokens returned; user.Roles contains Customer; one customer row persisted.','Email not yet registered.','Passed','02/11/2026','TanNXSE181599','IT - automated'),
      @('TC-AUTH-02','Register with a duplicate email','1. Register user A.`n2. POST /auth/register again with the same email.','409 Conflict with code email_exists; no second customer row is created.','User A already exists.','Passed','02/11/2026','TanNXSE181599','IT - automated'),
      @('TC-AUTH-03','Login with valid credentials','1. POST /auth/login with the registered email and password.','200 OK; access + refresh tokens; user.Id equals the stored customer id.','User registered and active.','Passed','02/11/2026','TanNXSE181599','IT - automated'),
      @('TC-AUTH-04','Login with a wrong password','1. POST /auth/login with the correct email and a wrong password.','401 Unauthorized with code unauthorized.','User registered.','Passed','02/11/2026','TanNXSE181599','IT - automated'),
      @('TC-AUTH-05','Login with an unknown email','1. POST /auth/login with an email that does not exist.','401 with the same message as TC-AUTH-04; account existence is not revealed.','No account for that email.','Passed','02/11/2026','TanNXSE181599','Security'),
      @('TC-AUTH-06','Refresh token rotation','1. POST /auth/refresh with a valid refresh token.`n2. Retry with the returned refresh token.','200; a new token pair is issued; the previous refresh token is rotated out.','Logged in.','Passed','02/11/2026','TanNXSE181599','IT - automated'),
      @('TC-AUTH-07','Access token used as a refresh token','1. POST /auth/refresh sending the access token.','401; token type is validated.','Logged in.','Passed','02/11/2026','TanNXSE181599','Security'),
      @('TC-AUTH-08','Google sign-in with a valid credential','1. POST /auth/google with a verified Google ID token and the configured audience.','200; a verified customer is created or linked by Google subject; session returned.','Google client id configured.','Passed','02/11/2026','TanNXSE181684','IT - automated'),
      @('TC-AUTH-09','Google sign-in with an invalid audience','1. POST /auth/google with a token issued for another audience.','401; the token is rejected server-side.','Google client id configured.','Passed','02/11/2026','TanNXSE181684','Security'),
      @('TC-AUTH-10','Forgot password for an unknown email','1. POST /auth/forgot-password with an unknown address.','200 with the same body as a known address; no email is sent.','No account.','Passed','02/11/2026','TanNXSE181599','Security'),
      @('TC-AUTH-11','Reset password with a valid token','1. Request a reset link.`n2. POST /auth/reset-password with the token and a new password.','200; password hash changes; token hash and expiry are cleared; session version increments.','A valid, unused reset token exists.','Passed','02/11/2026','TanNXSE181599','IT - automated'),
      @('TC-AUTH-12','Reset password token reuse','1. Repeat step 2 of TC-AUTH-11 with the same token.','400; the one-use token is rejected and the old password no longer works.','TC-AUTH-11 completed.','Passed','02/11/2026','TanNXSE181599','Security')
    )
  },
  @{
    Sheet='Model'; Name='Model Upload & Personal Library'; Req='FR-CUST-002, FR-CUST-003, FR-CUST-004, FR-CUST-010 - upload, validation, preview, library CRUD.';
    Cases=@(
      @('TC-MOD-01','Upload a supported STL model','1. POST /models with a multipart STL file <= 50 MB.`n2. Read the created model.','201; file stored; SHA-256 hash recorded; model owned by the caller.','Customer logged in; quota available.','Passed','02/11/2026','MinhNQSE181684','IT - automated'),
      @('TC-MOD-02','Upload an unsupported format','1. POST /models with a .txt file.','400 with a clear unsupported-format message; no model row created.','Customer logged in.','Passed','02/11/2026','MinhNQSE181684','IT - automated'),
      @('TC-MOD-03','Upload a file over 50 MB','1. POST /models with a 51 MB STL.','400/413; the upload is rejected before storage.','Customer logged in.','Passed','02/11/2026','MinhNQSE181684','Boundary'),
      @('TC-MOD-04','Geometry analysis of a cube','1. Upload a 20 mm cube.`n2. Read the analysis result.','Bounding box 20x20x20 mm (tolerance 0.1 mm); volume reported; watertight = true.','Model uploaded.','Passed','02/11/2026','TuanDNBSE185029','IT - automated'),
      @('TC-MOD-05','Validation of an open mesh','1. Upload an STL with a missing face.','Model accepted but flagged not watertight; the issue is listed.','Model file prepared.','Passed','02/11/2026','TuanDNBSE185029','IT - automated'),
      @('TC-MOD-06','In-browser 3D preview','1. Open the model detail screen.`n2. Rotate and zoom the preview.','The mesh renders; rotate/zoom work; dimensions match the analysis result.','Model uploaded.','Pending','','NhatTQSE181682','Manual UI - to execute'),
      @('TC-MOD-07','List models with search and tags','1. GET /models?search=cube.`n2. Inspect tags on each row.','Only matching models are returned; tags are mapped to the DTO.','At least two models, one named cube.','Passed','02/11/2026','MinhNQSE181684','IT - automated'),
      @('TC-MOD-08','Update an owned model','1. PUT /models/{id} changing name and tags.','200; only the caller-owned model is updated; tags are deduplicated.','Model owned by the caller.','Passed','02/11/2026','MinhNQSE181684','IT - automated'),
      @('TC-MOD-09','Update a foreign model','1. User B sends PUT /models/{id} for a model owned by user A.','404 Not Found; no mutation.','Two customers exist.','Passed','02/11/2026','MinhNQSE181684','Security'),
      @('TC-MOD-10','Delete own and foreign models','1. DELETE /models/{id} as the owner.`n2. DELETE another model as a non-owner.','Owner: 204; GET afterwards returns 404. Non-owner: 404.','Two customers; two models.','Passed','02/11/2026','MinhNQSE181684','Security')
    )
  },
  @{
    Sheet='Order'; Name='Print Configuration, Quote & Order'; Req='FR-CUST-005, FR-CUST-006, FR-CUST-007, FR-CUST-008, FR-CUST-012 - configuration, quote, checkout, staged payment.';
    Cases=@(
      @('TC-ORD-01','Configure print options','1. Open the configuration step.`n2. Try an invalid material/technology combination.','Invalid combinations are disabled with a reason; a valid configuration persists with the draft.','Model uploaded.','Pending','','NhatTQSE181682','Manual UI - to execute'),
      @('TC-ORD-02','Quote breakdown sums correctly','1. Request a quote.`n2. Sum every line item.','Line items plus shipping equal the quoted total; no rounding gap.','Configuration completed.','Passed','02/11/2026','TuanDNBSE185029','IT - automated'),
      @('TC-ORD-03','Delivery date comes from the engine','1. Request the same quote twice with a different network load.','The committed date changes with capacity; it is not a fixed lead-time table.','At least two labs active.','Passed','02/11/2026','TuanDNBSE185029','IT - automated'),
      @('TC-ORD-04','Quote expiry is enforced','1. Create a quote.`n2. Attempt checkout after the expiry window.','Checkout is refused with an expired-quote error.','Quote expired.','Passed','02/11/2026','TuanDNBSE185029','IT - automated'),
      @('TC-ORD-05','Checkout requires address and terms','1. POST /orders without a delivery address.`n2. Repeat without accepting the terms.','Both attempts are rejected with a validation error.','Quote valid.','Passed','02/11/2026','MinhNQSE181684','IT - automated'),
      @('TC-ORD-06','Order and quote are created in one transaction','1. Place an order.`n2. Inspect the order and its quote.','Both rows exist and reference each other; a forced failure leaves neither behind.','Quote valid.','Passed','02/11/2026','MinhNQSE181684','IT - automated'),
      @('TC-ORD-07','Deposit below the threshold','1. Report a payment below the configured deposit percent.','Order stays payment-pending; no production event is raised.','Order placed.','Passed','02/11/2026','MinhNQSE181684','IT - automated'),
      @('TC-ORD-08','Deposit reaches the threshold','1. Report a payment equal to the deposit percent.','Order is confirmed exactly once; the job-creation event is raised once.','Order placed.','Passed','02/11/2026','MinhNQSE181684','IT - automated'),
      @('TC-ORD-09','Settling the remaining balance','1. Pay the remaining balance.','Order becomes settled without a second confirmation event.','Deposit already paid.','Passed','02/11/2026','MinhNQSE181684','IT - automated'),
      @('TC-ORD-10','SEPay QR webhook and idempotency','1. Send a valid webhook twice with the same transaction id.`n2. Send a forged webhook.','Valid webhook commits once; the duplicate is ignored; the forged one never commits the order.','SEPay sandbox or bridge mode configured.','Pending','','MinhNQSE181684','Integration pending - mock/bridge mode')
    )
  },
  @{
    Sheet='Scheduling'; Name='Scheduling & Assignment Engine'; Req='FR-SCHED-003 to FR-SCHED-009 - capability filter, scoring, quote-time placement, rescheduling and decision log.';
    Cases=@(
      @('TC-SCH-01','Capability filter rejects a small build volume','1. Build a job larger than a candidate machine.`n2. Run the filter.','The machine is rejected with a build-volume reason; it never reaches scoring.','Lab and machine declared.','Passed','02/11/2026','TuanDNBSE185029','UT + IT'),
      @('TC-SCH-02','Filter rejects missing material or tolerance','1. Request PETG on a PLA-only machine.`n2. Request 0.05 mm on a 0.2 mm machine.','Both candidates are rejected with the matching constraint reason.','Machine specs declared.','Passed','02/11/2026','TuanDNBSE185029','UT'),
      @('TC-SCH-03','Filter excludes inactive and suspended labs','1. Mark a lab inactive.`n2. Mark another suspended.`n3. Run the filter.','Neither lab appears in the candidate set.','Two labs.','Passed','02/11/2026','TuanDNBSE185029','UT'),
      @('TC-SCH-04','Scoring stays in range and weights sum to one','1. Score a candidate set with the active weights.','Every score is between 0 and 1; the weights sum to 1.0.','Active scoring configuration.','Passed','02/11/2026','TuanDNBSE185029','UT'),
      @('TC-SCH-05','Scoring is deterministic for the same inputs and version','1. Score the same candidates twice with the same config version.','Both runs produce identical scores and the same ranking.','Active scoring configuration.','Passed','02/11/2026','TuanDNBSE185029','UT'),
      @('TC-SCH-06','Quote-time placement does not touch the real schedule','1. Run a quote-time trial with no free capacity.`n2. Compare the real timelines.','A date is returned; no job is placed; no machine timeline row changes.','Jobs already on the timeline.','Passed','02/11/2026','TuanDNBSE185029','UT'),
      @('TC-SCH-07','Job placement honours the internal due date','1. Place a 60-minute job on a free machine.','Internal due date >= start + print + post-processing; status becomes Assigned.','Free machine.','Passed','02/11/2026','TuanDNBSE185029','UT'),
      @('TC-SCH-08','Concurrent placement does not double-book','1. Place two jobs on the same machine in parallel.','Exactly one placement commits; the loser retries the next candidate.','One machine, two pending jobs.','Passed','02/11/2026','TuanDNBSE185029','IT - EXCLUDE constraint'),
      @('TC-SCH-09','Decline excludes the declining lab but keeps a fallback','1. Lab A declines the job.`n2. Run the repair.','Lab A is not offered the job again; another capable lab is chosen. If A is the only lab, A is still used to avoid late delivery.','Two labs, one decline.','Passed','02/11/2026','TuanDNBSE185029','UT'),
      @('TC-SCH-10','Reschedule never touches printing work','1. Start a job.`n2. Trigger an event-driven reschedule.','The in-progress job keeps its machine and dates; other unstarted jobs may move.','One in-progress job.','Passed','02/11/2026','TuanDNBSE185029','UT + IT'),
      @('TC-SCH-11','Decision log is written with the placement','1. Assign a job.`n2. Read the decision log.','One log row per committed placement with candidates, per-criterion scores, config version and time budget; both writes share one save.','Free machine.','Passed','02/11/2026','TuanDNBSE185029','UT + IT'),
      @('TC-SCH-12','Probe returns the earliest feasible date','1. Ask for the earliest date ignoring the current due date.`n2. Confirm nothing was saved.','The earliest feasible end is returned; the job stays Pending; no decision row is written.','A capable but busy machine.','Passed','02/11/2026','TuanDNBSE185029','UT')
    )
  },
  @{
    Sheet='Lab'; Name='Lab Registration & Production Workflow'; Req='FR-LAB-001 to FR-LAB-005 - registration, machine registry, stock, job response, completion and handover.';
    Cases=@(
      @('TC-LAB-01','Self-register a lab','1. Submit the lab registration form with name, city, hours, owner and capacity.','201; status Pending and IsActive false; the lab does not enter the capability filter.','A customer account.','Passed','02/11/2026','TanNXSE181599','UT'),
      @('TC-LAB-02','Admin approves a complete declaration','1. Add a machine and material stock.`n2. Approve the lab.','Status Active; IsActive true; the lab enters the filter.','Complete pending lab.','Passed','02/11/2026','TanNXSE181599','UT'),
      @('TC-LAB-03','Admin cannot approve an incomplete declaration','1. Approve a lab with no machine.`n2. Approve a lab with no material stock.','409 Conflict in both cases; the lab stays Pending.','Pending lab.','Passed','02/11/2026','TanNXSE181599','UT'),
      @('TC-LAB-04','Register a machine','1. POST the machine spec (technology, volume, tolerance, materials).`n2. Read the lab.','201; the machine is attached to the owned lab; unknown lab returns 404.','Approved lab.','Passed','02/11/2026','TanNXSE181599','UT'),
      @('TC-LAB-05','Change machine status','1. Set the machine to Maintenance.`n2. Attempt to delete it with an active job.','Status changes and is visible to the filter within 1 minute; deletion is blocked.','Machine with an active job.','Passed','02/11/2026','TanNXSE181599','UT'),
      @('TC-LAB-06','Declare material stock','1. Set PLA/BLACK stock to 1000 g.`n2. Complete a job consuming 200 g.','Available grams decrease; a stock transaction is recorded with a running total.','Approved lab.','Passed','02/11/2026','TanNXSE181599','UT'),
      @('TC-LAB-07','Accept a job within the response window','1. Receive a job offer.`n2. Accept within two hours.','Job becomes Assigned; the file hash is acknowledged; the reservation is created.','Offered job.','Passed','02/11/2026','TanNXSE181599','UT'),
      @('TC-LAB-08','Decline or timeout a job','1. Decline with a reason.`n2. Let another offer time out.','The job returns to Pending and is re-planned; the decline reason is stored.','Offered job.','Passed','02/11/2026','TanNXSE181599','UT')
    )
  },
  @{
    Sheet='Hub'; Name='Hub Inspection, Reprint & Shipping'; Req='FR-HUB-001 to FR-HUB-005 - batch receipt, QC, reprint, consolidation and shipment.';
    Cases=@(
      @('TC-HUB-01','Receive a lab batch','1. Scan the batch at the hub.`n2. Reconcile it against the expected jobs.','Every expected job is accounted for; missing jobs are flagged.','Batch handed over by the lab.','Passed','02/11/2026','MinhNQSE181684','IT'),
      @('TC-HUB-02','Missing or damaged item creates an incident','1. Mark one item MISSING and another DAMAGED.','Incidents are created and the lab and ops are notified the same day.','Batch received.','Passed','02/11/2026','MinhNQSE181684','IT'),
      @('TC-HUB-03','Inspection checklist with defect classification','1. Run the checklist and fail one item with a defect class and photos.','The failure stores defect class, attribution and photo evidence; the checklist version is saved.','Item in inspection.','Passed','02/11/2026','MinhNQSE181684','UT'),
      @('TC-HUB-04','Failed inspection triggers a reprint','1. Fail an item attributed to the lab.`n2. Trigger the reprint.','A reprint job is created that keeps the original deadline; the cost is charged to the lab.','Failed item.','Passed','02/11/2026','MinhNQSE181684','UT + IT'),
      @('TC-HUB-05','Third reprint escalates to ops','1. Trigger two reprints.`n2. Trigger the third.','The third attempt is not auto-created; it is routed to the operations queue.','The same item.','Passed','02/11/2026','MinhNQSE181684','UT + IT'),
      @('TC-HUB-06','Consolidation blocked until every item passes QC','1. Consolidate an order with one item still in production.','Packing is blocked and the outstanding item is reported.','Order with a pending item.','Passed','02/11/2026','MinhNQSE181684','UT'),
      @('TC-HUB-07','Ship requires a tracking number','1. Ship an order with an empty waybill.`n2. Ship with a waybill.','Empty: validation error. With waybill: status Shipping and the waybill stored.','Quality-checked order.','Passed','02/11/2026','MinhNQSE181684','UT'),
      @('TC-HUB-08','Customer confirmation starts the guarantee clock','1. Hub delivers the order.`n2. The customer confirms receipt.','DeliveredAt is recorded; the 30-day window starts only from the customer confirmation, not from hub delivery.','Shipped order.','Passed','02/11/2026','MinhNQSE181684','UT')
    )
  },
  @{
    Sheet='Admin'; Name='Administration & RBAC'; Req='FR-ADMIN-001, FR-ADMIN-002, FR-ADMIN-003 - accounts, roles, configuration and shared catalogues.';
    Cases=@(
      @('TC-ADM-01','Create a user and assign a role','1. POST /admin/users with a role.`n2. Log in as that user.','Account is created; the user only sees the menus allowed by the role.','SysAdmin session.','Passed','02/11/2026','TanNXSE181599','UT'),
      @('TC-ADM-02','RBAC blocks an unauthorized call','1. Call an admin endpoint with a Customer token.','403 Forbidden; no data is returned.','Two roles.','Passed','02/11/2026','TanNXSE181599','Security'),
      @('TC-ADM-03','Self-lockout is blocked','1. Remove the last admin role from the only SysAdmin.','The operation is refused; the account keeps its role.','Single SysAdmin.','Passed','02/11/2026','TanNXSE181599','UT'),
      @('TC-ADM-04','Update a shared catalogue item','1. Change the display name of a material.`n2. Request a new quote.','The change propagates to new quotes within one minute.','SysAdmin session.','Passed','02/11/2026','TanNXSE181599','UT'),
      @('TC-ADM-05','Deprecate an in-use catalogue item','1. Delete a material used by an active machine.','The item is deprecated, not deleted; existing references stay valid.','Material in use.','Passed','02/11/2026','TanNXSE181599','UT'),
      @('TC-ADM-06','Update pricing and scoring weights','1. Change the active weight set.`n2. Request a new quote and reschedule a job.','The new weights apply within one minute without restart; old quotes keep their frozen version.','SysAdmin session.','Pending','','TanNXSE181599','Manual UI - to execute'),
      @('TC-ADM-07','Audit log is append-only','1. Perform an admin action.`n2. Attempt to update or delete the audit row.','The action is logged; update and delete are refused.','SysAdmin session.','Passed','02/11/2026','TanNXSE181599','UT + IT'),
      @('TC-ADM-08','Admin configuration screens','1. Open the admin console.`n2. Change a threshold and save.','The screen loads current values and saves the new value with a success message.','SysAdmin session.','Pending','','NhatTQSE181682','Manual UI - to execute')
    )
  }
)

$defects = @(
  @('DF-001','Auth','Refresh token','Refresh returned 401 for a valid token because JWT inbound claims were mapped to ClaimTypes and the typ check failed.','High','TanNXSE181599','MinhNQSE181684','Fixed','02/10/2026'),
  @('DF-002','Model','Model tags','IReadOnlyCollection<string> tags were not supported by the EF primitive-collection migration.','High','MinhNQSE181684','MinhNQSE181684','Fixed','05/10/2026'),
  @('DF-003','Auth','Login / register','Surrounding whitespace in email and password caused a false credential mismatch.','Medium','TanNXSE181599','TanNXSE181599','Fixed','18/10/2026'),
  @('DF-004','Hub','Shipping gate','Auto-paid orders did not record PaidAmount, so hub shipping was blocked.','High','MinhNQSE181684','MinhNQSE181684','Fixed','27/10/2026'),
  @('DF-005','Model','Routing','The /models route was shadowed by the static asset folder.','Medium','NhatTQSE181682','NhatTQSE181682','Fixed','22/10/2026'),
  @('DF-006','Order','Order confirmation','Auto-confirmed orders and inventory role errors broke the confirmation flow.','Medium','MinhNQSE181684','MinhNQSE181684','Fixed','24/10/2026'),
  @('DF-007','Lab','Approval rule','Unit test FR_LAB_001_AC01 failed after BR-OPS-006 required a material declaration before approval; the fixture did not add stock.','Medium','TanNXSE181599','TanNXSE181599','Fixed','02/11/2026'),
  @('DF-008','Auth','Logout','Logout redirected to /orders instead of the home page.','Low','NhatTQSE181682','NhatTQSE181682','Fixed','01/11/2026')
)

$xlPasteFormats = -4122
$Excel = New-Object -ComObject Excel.Application
$Excel.Visible = $false
$Excel.DisplayAlerts = $false
$wb = $Excel.Workbooks.Open($path)

foreach ($ws in $wb.Worksheets) { try { $ws.Unprotect() } catch {} }

# ---------- Cover ----------
$cover = $wb.Worksheets.Item('Cover')
$cover.Range('B4').Value2 = $projectName
$cover.Range('B5').Value2 = $projectCode
$cover.Range('B6').Formula = '=B5&"_TestReport_v1.0"'
$cover.Range('F4').NumberFormat = '@'; $cover.Range('F4').Value2 = $creator
$cover.Range('F5').NumberFormat = '@'; $cover.Range('F5').Value2 = $issueDate
$cover.Range('F6').NumberFormat = '@'; $cover.Range('F6').Value2 = '1.0'
$cover.Range('A11').NumberFormat = '@'; $cover.Range('A11').Value2 = $issueDate
$cover.Range('B11').Value2 = '1.0'
$cover.Range('C11').Value2 = 'A'
$cover.Range('D11').Value2 = 'Initial creation of the Test Report'
$cover.Range('E11').Value2 = 'Report 5 - Software Test Documentation'
$cover.Range('F11').Value2 = 'Report5_Test Documentation.docx'

# ---------- Test Cases ----------
$tc = $wb.Worksheets.Item('Test Cases')
$tc.Range('D3').NumberFormat = 'General'; $tc.Range('D3').Formula = '=Cover!B4'
$tc.Range('D4').NumberFormat = 'General'; $tc.Range('D4').Formula = '=Cover!B5'
$tc.Range('D5').Value2 = "1. Server: PrintGrid API on .NET 9 (local Docker or Railway).`n2. Database: PostgreSQL 16 + Redis.`n3. Web browser: Chrome / Edge (latest).`n4. API client: Swagger UI or Postman.`n5. Test accounts: customer, lab owner, hub QC, sysadmin."
$tc.Range('B8').Value2 = 'No'
$tc.Range('C8').Value2 = 'Function Name'
$tc.Range('D8').Value2 = 'Sheet Name'
$tc.Range('E8').Value2 = 'Description'
$tc.Range('F8').Value2 = 'Pre-Condition'
$tc.Range('B9:F23').ClearContents()
$r = 9
foreach ($f in $features) {
    $tc.Cells.Item($r, 2).Formula = "$($r - 8)"
    $tc.Cells.Item($r, 3).Value2 = $f.Name
    $tc.Cells.Item($r, 4).Value2 = $f.Sheet
    $tc.Cells.Item($r, 5).Value2 = $f.Req
    $tc.Cells.Item($r, 6).Value2 = 'Test environment described above; module dependencies seeded.'
    $r++
}
$tc.Range('B8:F16').Borders.LineStyle = 1
$tc.Range('B8:F8').Font.Bold = $true

# ---------- Feature sheets ----------
$wb.Worksheets.Item('Feature 1').Name = 'Auth'
$wb.Worksheets.Item('Feature 2').Name = 'Model'
$copyNames = @('Order','Scheduling','Lab','Hub','Admin')
foreach ($n in $copyNames) {
    $last = $wb.Worksheets.Item($wb.Worksheets.Count)
    $wb.Worksheets.Item('Auth').Copy([Type]::Missing, $last)
    $wb.Worksheets.Item($wb.Worksheets.Count).Name = $n
}

foreach ($f in $features) {
    $ws = $wb.Worksheets.Item($f.Sheet)
    $ws.Range('B2').Value2 = $f.Name
    $ws.Range('B3').Value2 = $f.Req
    $ws.Range('A11:O400').ClearContents()

    # per-round counters must read the round column, not always column F
    $ws.Range('B4').Formula = '=COUNTIF($A$11:$A$400,"TC-*")'
    $ws.Range('B6').Formula = '=COUNTIF($F$11:$F$400,B$5)'
    $ws.Range('C6').Formula = '=COUNTIF($F$11:$F$400,C$5)'
    $ws.Range('D6').Formula = '=COUNTIF($F$11:$F$400,D$5)'
    $ws.Range('E6').Formula = '=COUNTIF($F$11:$F$400,E$5)'
    $ws.Range('B7').Formula = '=COUNTIF($I$11:$I$400,B$5)'
    $ws.Range('C7').Formula = '=COUNTIF($I$11:$I$400,C$5)'
    $ws.Range('D7').Formula = '=COUNTIF($I$11:$I$400,D$5)'
    $ws.Range('E7').Formula = '=COUNTIF($I$11:$I$400,E$5)'
    $ws.Range('B8').Formula = '=COUNTIF($L$11:$L$400,B$5)'
    $ws.Range('C8').Formula = '=COUNTIF($L$11:$L$400,C$5)'
    $ws.Range('D8').Formula = '=COUNTIF($L$11:$L$400,D$5)'
    $ws.Range('E8').Formula = '=COUNTIF($L$11:$L$400,E$5)'

    $row = 11
    foreach ($c in $f.Cases) {
        $ws.Cells.Item($row, 1).Value2  = $c[0]
        $ws.Cells.Item($row, 2).Value2  = $c[1]
        $ws.Cells.Item($row, 3).Value2  = $c[2]
        $ws.Cells.Item($row, 4).Value2  = $c[3]
        $ws.Cells.Item($row, 5).Value2  = $c[4]
        $ws.Cells.Item($row, 6).Value2  = $c[5]
        $ws.Cells.Item($row, 7).NumberFormat = '@'
        $ws.Cells.Item($row, 7).Value2  = $c[6]
        $ws.Cells.Item($row, 8).Value2  = $c[7]
        $ws.Cells.Item($row, 9).Value2  = $c[5]
        $ws.Cells.Item($row, 10).NumberFormat = '@'
        $ws.Cells.Item($row, 10).Value2 = $c[6]
        $ws.Cells.Item($row, 11).Value2 = $c[7]
        $ws.Cells.Item($row, 12).Value2 = $c[5]
        $ws.Cells.Item($row, 13).NumberFormat = '@'
        $ws.Cells.Item($row, 13).Value2 = $c[6]
        $ws.Cells.Item($row, 14).Value2 = $c[7]
        $ws.Cells.Item($row, 15).Value2 = $c[8]
        $row++
    }
    $lastRow = $row - 1
    $rng = $ws.Range("A11:O$lastRow")
    $rng.WrapText = $true
    $rng.VerticalAlignment = -4160
    $rng.Borders.LineStyle = 1
    $ws.Range('A11:O11').Font.Bold = $false
    $ws.Columns.Item(3).ColumnWidth = 45
    $ws.Columns.Item(4).ColumnWidth = 38
    $ws.Columns.Item(5).ColumnWidth = 30
    $ws.Columns.Item(15).ColumnWidth = 26
}

# ---------- Test Statistics ----------
$st = $wb.Worksheets.Item('Test Statistics')
$st.Range('C3').Value2 = $projectName
$st.Range('C4').Value2 = $projectCode
$st.Range('C5').Formula = '=C4&"_TestReport_v1.0"'
$st.Range('F3').Value2 = $creator
$st.Range('F4').Value2 = $reviewer
$st.Range('H5').NumberFormat = '@'; $st.Range('H5').Value2 = $issueDate
$st.Range('C6').Value2 = 'Release 1 includes 7 modules: Authentication, Model Library, Order & Quote, Scheduling, Lab, Hub and Administration.'

$st.Range('B11:H20').ClearContents()
$r = 11
foreach ($f in $features) {
    $st.Cells.Item($r, 2).Formula = "$($r - 10)"
    $st.Cells.Item($r, 3).Value2 = $f.Name
    $st.Cells.Item($r, 4).Formula = "='$($f.Sheet)'!B6"
    $st.Cells.Item($r, 5).Formula = "='$($f.Sheet)'!C6"
    $st.Cells.Item($r, 6).Formula = "='$($f.Sheet)'!D6"
    $st.Cells.Item($r, 7).Formula = "='$($f.Sheet)'!E6"
    $st.Cells.Item($r, 8).Formula = "='$($f.Sheet)'!B4"
    $r++
}
$subRow = $r
$st.Cells.Item($subRow, 3).Value2 = 'Sub total'
$st.Cells.Item($subRow, 4).Formula = "=SUM(D11:D$($subRow-1))"
$st.Cells.Item($subRow, 5).Formula = "=SUM(E11:E$($subRow-1))"
$st.Cells.Item($subRow, 6).Formula = "=SUM(F11:F$($subRow-1))"
$st.Cells.Item($subRow, 7).Formula = "=SUM(G11:G$($subRow-1))"
$st.Cells.Item($subRow, 8).Formula = "=SUM(H11:H$($subRow-1))"

$c1 = $subRow + 2
$st.Cells.Item($c1, 3).Value2 = 'Test coverage'
$st.Cells.Item($c1, 5).Formula = "=(D$subRow+E$subRow)*100/(H$subRow-G$subRow)"
$st.Cells.Item($c1, 6).Value2 = '%'
$st.Cells.Item($c1 + 1, 3).Value2 = 'Test successful coverage'
$st.Cells.Item($c1 + 1, 5).Formula = "=D$subRow*100/(H$subRow-G$subRow)"
$st.Cells.Item($c1 + 1, 6).Value2 = '%'

$st.Range("B10:H$($c1+1)").Borders.LineStyle = 1
$st.Range("B10:H10").Font.Bold = $true
$st.Range("C$subRow:H$subRow").Font.Bold = $true
$st.Range("D11:H$subRow").NumberFormat = '0'
$st.Range("E$c1:F$($c1+1)").NumberFormat = '0.00'

# ---------- Defects sheet ----------
$old = $wb.Worksheets | Where-Object { $_.Name -eq 'Defects' }
if ($old) { $old.Delete() }
$d = $wb.Worksheets.Add([Type]::Missing, $wb.Worksheets.Item($wb.Worksheets.Count))
$d.Name = 'Defects'
$headers = @('No','Defect ID','Module','Screen / Function','Description','Severity','Detected by','Assigned to','Status','Fixed date')
for ($i = 0; $i -lt $headers.Count; $i++) { $d.Cells.Item(1, $i + 1).Value2 = $headers[$i] }
$r = 2
foreach ($x in $defects) {
    $d.Cells.Item($r, 1).Formula = "$($r - 1)"
    for ($i = 0; $i -lt $x.Count; $i++) {
        $d.Cells.Item($r, $i + 2).Value2 = $x[$i]
    }
    $r++
}
$d.Range("A1:J$($r-1)").Borders.LineStyle = 1
$d.Range('A1:J1').Font.Bold = $true
$d.Columns.Item(5).ColumnWidth = 60
$d.Columns.Item(6).ColumnWidth = 10
$d.Range("A1:J$($r-1)").WrapText = $true

$wb.Save()
$wb.Close($true)
$Excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($Excel) | Out-Null
"TEST REPORT WORKBOOK FILLED"
