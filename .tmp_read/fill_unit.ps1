$ErrorActionPreference = 'Stop'
$path = 'E:\DOC DO An\2_SEP490\Report5_Unit Test.xls'

$projectName = 'PrintGrid - Distributed 3D Printing Fulfillment & Scheduling Platform'
$projectCode = 'FA26SE249'
$creator      = 'TanNXSE181599'
$reviewer     = 'Nguyen Tan Phuc'
$issueDate    = '02/11/2026'
$executedDate = '02/11/2026'
$klocNorm     = 30

function To-Sentence([string]$name) {
    $s = $name -replace '_', ' '
    $map = @{
        'stl' = 'STL'; 'obj' = 'OBJ'; 'glb' = 'GLB'; '3mf' = '3MF'; 'bbox' = 'bounding box';
        'qc' = 'QC'; 'hub' = 'Hub'; 'ops' = 'Ops'; 'id' = 'ID'; 'dto' = 'DTO'; 'ac01'='AC01';
        'ac02'='AC02'; 'ac03'='AC03'; 'fr'='FR'; 'us'='US'
    }
    foreach ($k in $map.Keys) { $s = $s -replace "(?i)\b$k\b", $map[$k] }
    if ($s.Length -gt 1 -and $s[1] -cmatch '[a-z]') { $s = $s.Substring(0,1).ToLower() + $s.Substring(1) }
    return $s
}

function Get-Type([string]$name) {
    $n = $name.ToLowerInvariant()
    if ($n -match 'boundary|two_hours|within|zero_to_one|exact|limit|reaches|partial|fully|second_time|only_once|below|exceeds|zero_stock|zero_to') { return 'B' }
    if ($n -match 'reject|fail|invalid|wrong|unknown|does_not|cannot|refused|blocked|requires|error|conflict|not_found|never|unsupported|open_mesh|without|negative|skips|skip|empty|positive') { return 'A' }
    return 'N'
}

$sheets = @(
    @{ Sheet='Auth'; Class='RegisterLoginTests'; Code='UT-AUTH-001'; Func='Register / Login / Password reset'; Req='FR-CUST-001'; LOC=325;
       Pre='Mock ICustomerRepository, IUnitOfWork, IPasswordHasher, ITokenService, IEmailService and IDistributedCache with NSubstitute; Arrange-Act-Assert.';
       Tests=@(
         'Register_creates_customer_and_returns_a_session',
         'Register_rejects_a_duplicate_email',
         'Login_returns_a_session_for_valid_credentials',
         'Login_rejects_a_known_email_with_a_wrong_password',
         'Login_does_not_reveal_whether_an_email_exists',
         'Google_login_creates_verified_customer_and_returns_session',
         'Forgot_password_does_not_reveal_unknown_email',
         'Reset_password_consumes_valid_token') },
    @{ Sheet='Model'; Class='ModelCrudTests'; Code='UT-MODEL-001'; Func='Model library CRUD'; Req='FR-CUST-002, FR-CUST-010'; LOC=144;
       Pre='Mock IModelRepository and IUnitOfWork; a model owned by the acting customer exists; foreign models belong to another owner.';
       Tests=@(
         'Create_returns_a_dto_and_persists',
         'Create_normalizes_format_to_uppercase_and_deduplicates_tags',
         'Update_edits_only_a_model_owned_by_the_customer',
         'Update_returns_not_found_when_the_model_belongs_to_someone_else',
         'Delete_removes_a_owned_model',
         'Delete_returns_not_found_for_foreign_model',
         'GetModels_applies_search_and_maps_tags') },
    @{ Sheet='Order'; Class='OrderTests'; Code='UT-ORDER-001'; Func='Order state machine'; Req='FR-CUST-007, FR-CUST-008, FR-HUB-005'; LOC=298;
       Pre='An order aggregate seeded from a confirmed quote; every transition is attempted in isolation.';
       Tests=@(
         'ConfirmPayment_moves_a_pending_order_to_confirmed_and_raises_the_event',
         'ConfirmPayment_is_rejected_the_second_time',
         'Order_cannot_skip_straight_from_confirmed_to_shipping',
         'Failed_inspection_can_send_a_quality_check_order_back_into_production',
         'Hub_delivery_does_NOT_start_the_guarantee_clock',
         'Ship_requires_a_tracking_number',
         'Ship_moves_a_quality_checked_order_to_shipping_and_stores_the_tracking',
         'Ship_is_rejected_before_the_order_is_quality_checked',
         'Deliver_requires_the_order_to_be_shipped_first',
         'Customer_confirmation_requires_the_order_to_be_delivered',
         'Customer_confirmation_stamps_the_guarantee_anchor_and_who_confirmed',
         'Refund_cannot_exceed_the_amount_actually_paid',
         'Refunding_everything_paid_marks_the_order_as_fully_refunded',
         'Partial_refund_is_not_considered_fully_refunded',
         'An_unpaid_order_is_never_fully_refunded',
         'Order_copies_every_quote_item_with_its_configuration') },
    @{ Sheet='Commerce'; Class='CommerceFlowTests'; Code='UT-PAY-001'; Func='Staged payment and refunds'; Req='FR-CUST-008, FR-CUST-012'; LOC=415;
       Pre='Order with a frozen quote total; deposit / balance thresholds configured on the aggregate under test.';
       Tests=@(
         'Payment_below_the_deposit_keeps_the_order_payment_pending_and_raises_no_event',
         'Reaching_the_deposit_confirms_the_order_once_and_raises_the_job_event',
         'Settling_the_remaining_balance_marks_the_order_settled_without_a_second_confirmation',
         'Payment_cannot_exceed_the_remaining_balance',
         'Payment_must_be_positive',
         'Pending_refund_can_be_approved_only_once',
         'Pending_refund_can_be_rejected',
         'Design_request_starts_pending_and_keeps_the_reference_image_keys',
         'Design_request_status_only_accepts_the_documented_lifecycle') },
    @{ Sheet='SchedFilter'; Class='CapabilityFilterTests'; Code='UT-SCHED-003'; Func='Capability filter'; Req='FR-SCHED-003'; LOC=59;
       Pre='A set of labs and machines with declared technology, build volume, tolerance, materials and stock.';
       Tests=@(
         'Filter_keeps_machine_that_satisfies_every_constraint',
         'Filter_rejects_machine_whose_build_volume_is_too_small',
         'Filter_rejects_machine_that_cannot_hold_the_requested_tolerance',
         'Filter_rejects_machine_without_the_requested_material',
         'Filter_skips_inactive_labs_entirely',
         'Filter_rejects_exact_material_when_requested_color_has_zero_stock',
         'Filter_rejects_stock_that_cannot_cover_estimated_consumption',
         'Filter_rejects_suspended_lab_before_scoring',
         'Adversarial_suite_never_leaks_an_infeasible_candidate') },
    @{ Sheet='SchedScore'; Class='AssignmentScorerTests'; Code='UT-SCHED-004'; Func='Multi-criteria scoring'; Req='FR-SCHED-004'; LOC=66;
       Pre='Two or more feasible candidates; the active scoring weight set is supplied by the configuration provider.';
       Tests=@(
         'Score_prefers_the_lab_with_more_slack_before_the_due_date',
         'Score_prefers_the_more_reliable_lab_when_timing_is_equal',
         'Score_prefers_the_less_loaded_machine_when_everything_else_matches',
         'Score_stays_within_the_zero_to_one_range',
         'Weights_sum_to_one',
         'Changing_the_weight_period_changes_the_next_ranking') },
    @{ Sheet='SchedEngine'; Class='AssignmentEngineTests'; Code='UT-SCHED-006'; Func='Assignment engine'; Req='FR-SCHED-006, FR-SCHED-007, FR-SCHED-009'; LOC=320;
       Pre='Mock job, lab, timeline, decision-log repositories and IUnitOfWork; the clock is fixed at 2026-10-06T08:00Z.';
       Tests=@(
         'Assign_skips_a_lab_that_has_reached_its_ops_capacity_limit',
         'Assign_does_not_offer_the_job_back_to_the_lab_that_declined_it',
         'Assign_reads_the_active_weight_period_for_each_new_assignment',
         'Assign_keeps_the_only_capable_lab_even_when_it_is_the_one_that_declined',
         'Assign_writes_the_decision_log_in_the_same_save_as_the_placement',
         'Assign_retries_the_next_candidate_when_the_database_rejects_the_first_placement',
         'Assign_commits_the_best_ranked_fallback_even_with_no_time_budget_left',
         'Assign_stops_the_search_and_flags_the_budget_when_it_is_spent',
         'Assign_reports_a_shortfall_when_no_capable_machine_fits_before_the_due_date',
         'Assign_logs_a_rejection_when_no_lab_can_print_the_specification',
         'A_probe_answers_with_the_earliest_feasible_date_without_touching_the_schedule',
         'Assign_refuses_a_job_that_is_already_printing') },
    @{ Sheet='JobLifecycle'; Class='JobLifecycleTests'; Code='UT-SCHED-005'; Func='Job lifecycle and QC proof'; Req='FR-LAB-004, FR-LAB-005, FR-HUB-002, FR-HUB-003'; LOC=461;
       Pre='A job assigned to a lab machine; staff approval state and proof photos are controlled per case.';
       Tests=@(
         'Complete_requires_self_report_and_proof_photos',
         'Complete_submits_qc_proof_for_staff_review',
         'Approve_qc_proof_records_audit_and_unlocks_hub_inspection',
         'Reject_qc_proof_requires_reason_and_returns_job_for_rework',
         'Decline_transitions_job_back_to_pending_and_clears_assignment',
         'Accept_within_two_hours_succeeds',
         'Accept_after_two_hours_fails_and_auto_declines_to_pending',
         'TimeoutAcceptance_after_two_hours_auto_declines_to_pending',
         'TimeoutAcceptance_before_two_hours_fails',
         'Ops_reassignment_requires_reason_and_only_allows_unstarted_work',
         'Validator_fails_when_no_photo_evidence_provided_AC01',
         'Validator_fails_when_checklist_item_has_invalid_status_AC02',
         'Validator_fails_when_failed_inspection_lacks_fault_attribution_AC03',
         'Job_Fail_records_the_failure_and_asks_for_a_replan_FR_HUB_003_AC01',
         'Job_FailInspectionCustomerFault_emits_notification_event_FR_HUB_003_AC02',
         'Job_MarkInspectionPassed_completes_job_and_emits_event',
         'Job_MarkInspectionPassed_is_refused_until_the_lab_proof_is_approved',
         'Completion_requires_positive_actual_time_and_material') },
    @{ Sheet='Slicing'; Class='SlicingServiceTests'; Code='UT-SCHED-002'; Func='Geometry analysis and slicing estimate'; Req='FR-SCHED-001, FR-SCHED-002'; LOC=164;
       Pre='Fixture streams for binary STL, ASCII STL, OBJ, 3MF, GLB and malformed meshes.';
       Tests=@(
         'AnalyzeAsync_binary_stl_cube_returns_bbox_and_volume',
         'AnalyzeAsync_ascii_stl_cube_returns_bbox_and_volume',
         'AnalyzeAsync_obj_cube_returns_bbox_and_volume',
         'AnalyzeAsync_unsupported_format_returns_failed',
         'AnalyzeAsync_valid_3mf_returns_mesh_statistics',
         'AnalyzeAsync_glb_cube_returns_bbox_and_volume',
         'AnalyzeAsync_glb_authored_in_metres_is_read_as_millimetres',
         'AnalyzeAsync_glb_without_container_magic_returns_failed',
         'AnalyzeAsync_leaves_the_callers_stream_open',
         'AnalyzeAsync_open_mesh_reports_not_watertight',
         'Estimate_computes_minutes_and_grams_for_reference_config',
         'Estimate_material_density_changes_grams_but_not_minutes',
         'Estimate_higher_resolution_prints_longer',
         'EstimateFromMetadata_is_deterministic_and_scales_with_file_size') },
    @{ Sheet='Shipment'; Class='ShipmentGateTests'; Code='UT-HUB-005'; Func='Shipment gate'; Req='FR-HUB-004, FR-HUB-005, FR-HUB-006'; LOC=298;
       Pre='Order whose items carry a status snapshot; the QC gate can be enabled or disabled by configuration.';
       Tests=@(
         'Ship_is_blocked_while_any_item_is_still_Pending',
         'Ship_is_blocked_while_any_item_is_only_in_QualityCheck',
         'Ship_succeeds_when_every_item_is_Completed',
         'Ship_is_blocked_when_the_order_was_fully_refunded',
         'Consolidated_ship_is_blocked_when_one_member_has_an_item_not_Completed',
         'Completed_production_signal_sets_the_item_status_without_moving_the_order',
         'Completed_signal_keeps_a_quality_check_order_in_quality_check',
         'Gates_off_lets_an_order_ship_with_items_that_never_passed_QC',
         'Gates_off_still_refuses_a_tracking_number_free_ship',
         'Gates_off_still_refuses_to_jump_the_state_machine') },
    @{ Sheet='LabRegistry'; Class='LabRegistryTests'; Code='UT-LAB-001'; Func='Lab and machine registry'; Req='FR-LAB-001, FR-LAB-002'; LOC=380;
       Pre='Mock ILabRepository and IUnitOfWork; lab declarations cover complete and incomplete profiles.';
       Tests=@(
         'RegisterLab_creates_and_saves_active_lab',
         'RegisterMachine_adds_machine_to_existing_lab',
         'RegisterMachine_unknown_lab_fails_with_not_found',
         'RegisterLab_with_owner_waits_for_admin_activation_and_can_add_machine',
         'SetMachineStatus_changes_status_on_owned_machine',
         'Admin_cannot_activate_an_incomplete_lab_declaration',
         'Admin_can_activate_a_complete_lab_declaration',
         'DeleteMachine_is_blocked_when_machine_has_active_jobs') }
)

$xlDown = -4121
$xlPasteFormats = -4122
$xlPasteValues = -4163

$Excel = New-Object -ComObject Excel.Application
$Excel.Visible = $false
$Excel.DisplayAlerts = $false
$wb = $Excel.Workbooks.Open($path)

foreach ($ws in $wb.Worksheets) { try { $ws.Unprotect() } catch {} }

# ---------- Cover ----------
$cover = $wb.Worksheets.Item('Cover')
$cover.Range('B4').Value2 = $projectName
$cover.Range('B5').Value2 = $projectCode
$cover.Range('B6').Formula = '=B5&"_UnitTest_v1.0"'
$cover.Range('F4').NumberFormat = '@'
$cover.Range('F5').NumberFormat = '@'
$cover.Range('F6').NumberFormat = '@'
$cover.Range('F4').Value2 = $creator
$cover.Range('F5').Value2 = $issueDate
$cover.Range('F6').Value2 = '1.0'
$cover.Range('A11').Value2 = $issueDate
$cover.Range('B11').Value2 = '1.0'
$cover.Range('C11').Value2 = 'A'
$cover.Range('D11').Value2 = 'Initial creation of the Unit Test document'
$cover.Range('E11').Value2 = 'Report 5 - Software Test Documentation'
$cover.Range('F11').Value2 = 'Report5_Test Documentation.docx'

# ---------- Functions ----------
$functions = $wb.Worksheets.Item('Functions')
$functions.Range('E4').Formula = "=Cover!B4"
$functions.Range('E5').Formula = "=Cover!B5"
$functions.Range('E6').Formula = "$klocNorm"
$functions.Range('E7').Value2 = 'Windows 11 / .NET 9 SDK / xUnit 2.x / NSubstitute / FluentAssertions; tests run with dotnet test.'

# clear old sample rows
$functions.Range('A11:H23').ClearContents()

$row = 11
foreach ($s in $sheets) {
    $functions.Cells.Item($row, 1).Formula = "$($row - 10)"
    $functions.Cells.Item($row, 2).Value2 = $s.Req
    $functions.Cells.Item($row, 3).Value2 = $s.Class
    $functions.Cells.Item($row, 4).Value2 = $s.Func
    $functions.Cells.Item($row, 5).Value2 = $s.Code
    $functions.Cells.Item($row, 6).Value2 = $s.Sheet
    $functions.Cells.Item($row, 7).Value2 = 'Unit test cases for ' + $s.Func
    $functions.Cells.Item($row, 8).Value2 = $s.Pre
    $row++
}

# ---------- Function sheets ----------
# rename the three template sheets
$wb.Worksheets.Item('Function 1').Name = 'Auth'
$wb.Worksheets.Item('Function 2').Name = 'Model'
$wb.Worksheets.Item('Function3').Name = 'Order'

# copy Order for the remaining sheets
$names = @('Commerce','SchedFilter','SchedScore','SchedEngine','JobLifecycle','Slicing','Shipment','LabRegistry')
foreach ($n in $names) {
    $last = $wb.Worksheets.Item($wb.Worksheets.Count)
    $wb.Worksheets.Item('Order').Copy([Type]::Missing, $last)
    $wb.Worksheets.Item($wb.Worksheets.Count).Name = $n
}

$rowIndex = 11
foreach ($s in $sheets) {
    $ws = $wb.Worksheets.Item($s.Sheet)
    $n = $s.Tests.Count

    # metadata
    $ws.Range('C2').NumberFormat = 'General'
    $ws.Range('L2').NumberFormat = 'General'
    $ws.Range('C2').Formula = "=Functions!E$rowIndex"
    $ws.Range('L2').Formula = "=Functions!D$rowIndex"
    $ws.Range('C3').Value2 = $creator
    $ws.Range('L3').Value2 = $creator
    $ws.Range('C4').Formula = "$($s.LOC)"
    $ws.Range('C5').Value2 = 'Verify ' + $s.Func + ' (' + $s.Req + '). ' + $s.Pre

    # clear template body from column F onwards
    $ws.Range('F9:HT42').ClearContents()
    $ws.Range('C11:E38').ClearContents()
    # make sure the last used column is within our range
    $lastCol = 5 + $n   # E is 5; UTCID01 starts at F=6
    $lastColLetter = $ws.Cells.Item(1, $lastCol).Address($true, $false) -replace '\$','' -replace '\d',''

    # UTCID headers
    for ($i = 0; $i -lt $n; $i++) {
        $ws.Cells.Item(9, 6 + $i).Value2 = ('UTCID{0:D2}' -f ($i + 1))
    }

    # precondition
    $ws.Range('F11').Value2 = $s.Pre

    # labels
    $ws.Range('A14').Value2 = 'Input'
    $ws.Range('B14').Value2 = 'Test method'
    $ws.Range('A31').Value2 = 'Confirm'
    $ws.Range('B31').Value2 = 'Return'

    # test method + expected + type + result
    for ($i = 0; $i -lt $n; $i++) {
        $col = 6 + $i
        $name = $s.Tests[$i]
        $ws.Cells.Item(15, $col).Value2 = $name
        $ws.Cells.Item(32, $col).Value2 = To-Sentence $name
        $ws.Cells.Item(39, $col).Value2 = Get-Type $name
        $ws.Cells.Item(40, $col).Value2 = 'P'
        $ws.Cells.Item(41, $col).NumberFormat = '@'
        $ws.Cells.Item(41, $col).Value2 = $executedDate
    }

    # formatting for the used test columns
    $rng = $ws.Range($ws.Cells.Item(9, 6), $ws.Cells.Item(42, $lastCol))
    $rng.WrapText = $true
    $rng.VerticalAlignment = -4160
    $ws.Range($ws.Cells.Item(9, 6), $ws.Cells.Item(9, $lastCol)).HorizontalAlignment = -4108
    $ws.Rows.Item('15:15').RowHeight = 60
    $ws.Rows.Item('32:32').RowHeight = 60
    $ws.Rows.Item('39:42').RowHeight = 20
    $ws.Range($ws.Cells.Item(9, 6), $ws.Cells.Item(42, $lastCol)).Borders.LineStyle = 1

    $rowIndex++
}

# ---------- Statistics ----------
$stats = $wb.Worksheets.Item('Statistics')
$stats.Range('B4').Formula = '=Cover!B4'
$stats.Range('B5').Formula = '=Cover!B5'
$stats.Range('B6').Formula = '=B5&"_UnitTest_v1.0"'
$stats.Range('F4').Value2 = $creator
$stats.Range('F5').Value2 = $reviewer
$stats.Range('F6').NumberFormat = '@'
$stats.Range('F6').Value2 = $issueDate
$stats.Range('B7').Value2 = 'Release 1 covers 11 core functions (117 unit test cases). The full automated suite contains 253 cases across 37 test classes; the remaining 136 cases are executed by dotnet test and attached as a run log.'

$stats.Range('A12:I23').ClearContents()
$r = 12
foreach ($s in $sheets) {
    $stats.Cells.Item($r, 1).Formula = "$($r - 11)"
    $stats.Cells.Item($r, 2).Value2 = $s.Code
    $stats.Cells.Item($r, 3).Formula = "='$($s.Sheet)'!A7"
    $stats.Cells.Item($r, 4).Formula = "='$($s.Sheet)'!C7"
    $stats.Cells.Item($r, 5).Formula = "='$($s.Sheet)'!F7"
    $stats.Cells.Item($r, 6).Formula = "='$($s.Sheet)'!L7"
    $stats.Cells.Item($r, 7).Formula = "='$($s.Sheet)'!M7"
    $stats.Cells.Item($r, 8).Formula = "='$($s.Sheet)'!N7"
    $stats.Cells.Item($r, 9).Formula = "='$($s.Sheet)'!O7"
    $r++
}
$subRow = $r
$stats.Cells.Item($subRow, 2).Value2 = 'Sub total'
$stats.Cells.Item($subRow, 3).Formula = "=SUM(C12:C$($subRow-1))"
$stats.Cells.Item($subRow, 4).Formula = "=SUM(D12:D$($subRow-1))"
$stats.Cells.Item($subRow, 5).Formula = "=SUM(E12:E$($subRow-1))"
$stats.Cells.Item($subRow, 6).Formula = "=SUM(F12:F$($subRow-1))"
$stats.Cells.Item($subRow, 7).Formula = "=SUM(G12:G$($subRow-1))"
$stats.Cells.Item($subRow, 8).Formula = "=SUM(H12:H$($subRow-1))"
$stats.Cells.Item($subRow, 9).Formula = "=SUM(I12:I$($subRow-1))"

$c1 = $subRow + 2
$labels = @('Test coverage','Test successful coverage','Normal case','Abnormal case','Boundary case')
$formulas = @(
    "=(C$subRow+D$subRow)*100/I$subRow",
    "=C$subRow*100/I$subRow",
    "=F$subRow*100/I$subRow",
    "=G$subRow*100/I$subRow",
    "=H$subRow*100/I$subRow"
)
for ($i = 0; $i -lt 5; $i++) {
    $stats.Cells.Item($c1 + $i, 2).Value2 = $labels[$i]
    $stats.Cells.Item($c1 + $i, 4).Formula = $formulas[$i]
    $stats.Cells.Item($c1 + $i, 5).Value2 = '%'
}

# borders / formats
$stats.Range("C12:I$subRow").NumberFormat = '0'
$stats.Range("A11:I$c1").Borders.LineStyle = 1
$stats.Range('A11:I11').Font.Bold = $true
$stats.Range("B$subRow:I$subRow").Font.Bold = $true
$stats.Range("D$c1").NumberFormat = '0.00'
$stats.Range("D$($c1+1)").NumberFormat = '0.00'
$stats.Range("D$($c1+2)").NumberFormat = '0.00'
$stats.Range("D$($c1+3)").NumberFormat = '0.00'
$stats.Range("D$($c1+4)").NumberFormat = '0.00'

$wb.Save()
$wb.Worksheets.Item('Example').Move([Type]::Missing, $wb.Worksheets.Item($wb.Worksheets.Count))
$wb.Save()
$wb.Close($true)
$Excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($Excel) | Out-Null
"UNIT WORKBOOK FILLED"
