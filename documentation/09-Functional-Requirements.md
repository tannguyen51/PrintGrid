# Functional Requirements - PrintGrid Platform

## 1. Customer Module (FR-CUST)

### FR-CUST-001: User Registration and Authentication
**Priority:** High  
**Description:** Customers must be able to register and authenticate to access the platform.

**Requirements:**
- Register with email, password, full name
- Email verification required
- Login with email and password
- Password reset functionality
- Session management
- Remember me option

**Acceptance Criteria:**
- User can create account and receive verification email
- User can login with verified credentials
- Password meets security requirements (min 8 chars, uppercase, lowercase, number)
- Failed login attempts tracked and limited

---

### FR-CUST-002: 3D Model Upload
**Priority:** Critical  
**Description:** Customers can upload 3D model files in standard formats.

**Requirements:**
- Support STL, OBJ, 3MF formats
- File size limit: 50 MB
- Upload progress indicator
- Multi-file upload for batch orders
- Model stored securely in MinIO

**Acceptance Criteria:**
- Valid files upload successfully
- Invalid formats rejected with clear error
- Upload progress shown in real-time
- Files stored and retrievable

---

### FR-CUST-003: 3D Model Preview
**Priority:** High  
**Description:** Customers can preview uploaded models in browser before ordering.

**Requirements:**
- Interactive 3D viewer (rotate, zoom, pan)
- Display bounding box dimensions
- Show model statistics (vertices, faces)
- Wireframe and solid view modes

**Acceptance Criteria:**
- Model renders correctly in browser
- Controls responsive and intuitive
- Dimensions displayed accurately
- Works on desktop and tablet

---

### FR-CUST-004: Model Validation
**Priority:** Critical  
**Description:** System validates model geometry and provides feedback.

**Requirements:**
- Check mesh watertightness
- Detect non-manifold edges
- Validate build volume feasibility
- Suggest auto-repair for common issues

**Acceptance Criteria:**
- Valid models pass validation
- Invalid models rejected with specific issues listed
- Auto-repair offers for fixable issues
- Validation completes within 10 seconds

---

### FR-CUST-005: Print Configuration
**Priority:** Critical  
**Description:** Customers configure print parameters for their model.

**Requirements:**
- Material selection (PLA, PETG, ABS, etc.)
- Color selection
- Quality grade (Draft, Standard, High, Ultra)
- Infill density (10%, 30%, 50%, 100%)
- Quantity
- Post-processing options
- Required delivery date

**Acceptance Criteria:**
- All configuration options visible and selectable
- Unavailable combinations disabled with explanation
- Configuration saved with model
- Can modify before ordering

---

### FR-CUST-006: Automated Quoting
**Priority:** Critical  
**Description:** System generates quotes with price and delivery date.

**Requirements:**
- Async processing for complex models
- Progress indicator during quote generation
- Price breakdown display
- Committed delivery date from trial placement
- Quote expiry time shown
- Save quote for later

**Acceptance Criteria:**
- Quote generated within 60 seconds
- Price accurate based on configuration
- Delivery date realistic and achievable
- Quote expiry enforced

---

### FR-CUST-007: Order Placement
**Priority:** Critical  
**Description:** Customers can review and confirm orders.

**Requirements:**
- Review cart before checkout
- Apply promo codes
- Add delivery address
- Select shipping method
- Terms and conditions acceptance

**Acceptance Criteria:**
- Order summary shows all details correctly
- Cannot proceed without required info
- Validation prevents invalid orders

---

### FR-CUST-008: Payment Processing
**Priority:** Critical  
**Description:** Secure payment processing for orders.

**Requirements:**
- Integration with payment provider (Stripe test mode)
- Support credit/debit cards
- Payment confirmation
- Failed payment handling
- Receipt generation

**Acceptance Criteria:**
- Payment processed securely
- Confirmation displayed immediately
- Failed payments handled gracefully
- Receipt emailed to customer

---

### FR-CUST-009: Order Tracking
**Priority:** High  
**Description:** Real-time order status visibility.

**Requirements:**
- Order status stages displayed
- Estimated completion time
- Real-time updates via SignalR
- Email notifications on status change
- No visibility of which lab is producing

**Acceptance Criteria:**
- Status updates reflect actual progress
- Notifications received timely
- Estimated times update dynamically
- Lab identity hidden from customer

---

### FR-CUST-010: Model Library
**Priority:** Medium  
**Description:** Manage previously uploaded models.

**Requirements:**
- View all uploaded models
- Thumbnail previews
- Model metadata (name, dimensions, upload date)
- Delete models
- Reorder from library

**Acceptance Criteria:**
- Library shows all customer's models
- Thumbnails load quickly
- Reorder pre-fills configuration
- Deletion requires confirmation

---

### FR-CUST-011: Reprint/Complaint Request
**Priority:** Medium  
**Description:** Request reprint or raise quality complaints.

**Requirements:**
- Request form with issue description
- Photo upload capability
- Reason selection
- Reference original order
- Track request status

**Acceptance Criteria:**
- Request submitted successfully
- Photos attached and viewable
- Customer receives acknowledgment
- Status trackable

---

## 2. Lab Module (FR-LAB)

### FR-LAB-001: Lab Registration
**Priority:** High  
**Description:** Labs can register to join the network.

**Requirements:**
- Lab name, location, contact info
- Business hours and working calendar
- Distance/transfer time to hub
- Bank account details
- Documentation upload (business license, etc.)

**Acceptance Criteria:**
- Registration form validates input
- Submission triggers admin review
- Lab notified of approval status

---

### FR-LAB-002: Machine Registry
**Priority:** Critical  
**Description:** Labs maintain catalog of their machines.

**Requirements:**
- Add/edit/delete machines
- Specify: technology, brand, model, build volume, layer height range, tolerance
- Mark machine status (operational, maintenance, offline)
- Schedule maintenance windows
- View machine utilization

**Acceptance Criteria:**
- Machines listed with full specifications
- Status changes reflected in assignment
- Cannot delete machine with active jobs

---

### FR-LAB-003: Material Inventory
**Priority:** Critical  
**Description:** Track material stock levels.

**Requirements:**
- Add material types and colors
- Record quantity in stock
- Low stock alerts
- Consumption tracking per job
- Restock history

**Acceptance Criteria:**
- Inventory reflects current stock
- Alerts triggered at reorder point
- Consumption auto-updated on job completion

---

### FR-LAB-004: Job Assignment Notification
**Priority:** Critical  
**Description:** Receive and review assigned jobs.

**Requirements:**
- Real-time notification of assignment
- View job details (model preview, config, due date)
- Download model file (time-limited)
- Accept or reject with reason
- Response deadline enforced

**Acceptance Criteria:**
- Notifications received immediately
- Job details complete and accurate
- Model downloadable within time window
- Acceptance/rejection recorded

---

### FR-LAB-005: Production Workflow
**Priority:** Critical  
**Description:** Operators manage job execution.

**Requirements:**
- View machine queue
- Start job (marks in progress)
- Update status during production
- Report completion with actual time and material
- Report incidents with photos

**Acceptance Criteria:**
- Queue shows jobs in priority order
- Status updates reflected system-wide
- Actual data captured for calibration
- Incidents logged immediately

---

### FR-LAB-006: Schedule Visualization
**Priority:** Medium  
**Description:** View proposed schedule timeline.

**Requirements:**
- Machine-level Gantt chart
- Color-coded by priority
- Show job IDs and durations
- Filter by machine or material
- Print/export schedule

**Acceptance Criteria:**
- Timeline accurate and readable
- Updates after rescheduling
- Interactive (click for details)

---

### FR-LAB-007: Performance Dashboard
**Priority:** Medium  
**Description:** View own performance metrics.

**Requirements:**
- Display: ODR, FPY, acceptance rate, utilization
- Trend charts over time
- Comparison to network average
- Defect breakdown
- Revenue summary

**Acceptance Criteria:**
- Metrics calculated correctly
- Charts render clearly
- Network comparison anonymized
- Updates daily

---

## 3. Hub Module (FR-HUB)

### FR-HUB-001: Batch Receipt
**Priority:** High  
**Description:** Receive incoming shipments from labs.

**Requirements:**
- Scan batch barcode
- View expected jobs in batch
- Scan individual job QR codes
- Reconcile received vs expected
- Report discrepancies

**Acceptance Criteria:**
- Scanning captures job IDs correctly
- Discrepancies flagged immediately
- Labs notified of issues

---

### FR-HUB-002: Quality Inspection
**Priority:** Critical  
**Description:** Inspect parts per quality checklist.

**Requirements:**
- Load checklist for job's quality grade
- Mark each check pass/fail
- Capture photos (multiple angles)
- Overall pass/fail decision
- Classify defect if fail
- Attribute fault (lab/hub/customer)

**Acceptance Criteria:**
- Checklist items mandatory
- Photos required for failures
- Fault attribution rules enforced
- Decision triggers appropriate action

---

### FR-HUB-003: Reprint Management
**Priority:** High  
**Description:** Initiate reprints for failed parts.

**Requirements:**
- Auto-create reprint job on lab fault
- Set priority to URGENT
- Inherit original deadline
- Notify customer if delay expected
- Track reprint status

**Acceptance Criteria:**
- Reprint job created automatically
- Assigned via normal allocation
- Customer kept informed

---

### FR-HUB-004: Order Consolidation
**Priority:** High  
**Description:** Combine items for fulfillment.

**Requirements:**
- View orders ready for packing
- Check all items passed inspection
- Print packing slip
- Record package details
- Generate shipping label
- Scan to carrier

**Acceptance Criteria:**
- Only complete orders packed
- Packing slip accurate
- Tracking number captured
- Customer notified of shipment

---

## 4. Scheduling and Assignment Module (FR-SCHED)

### FR-SCHED-001: Geometry Analysis
**Priority:** Critical  
**Description:** Analyze uploaded models for scheduling inputs.

**Requirements:**
- Parse STL/OBJ/3MF files
- Calculate bounding box
- Compute volume
- Check mesh integrity

**Acceptance Criteria:**
- Analysis completes in <10s
- Dimensions accurate to 0.1mm
- Detects common issues

---

### FR-SCHED-002: Slicing Service
**Priority:** Critical  
**Description:** Generate toolpaths and estimates.

**Requirements:**
- Integrate CuraEngine or PrusaSlicer
- Map configuration to slicer profile
- Execute slicing asynchronously
- Extract print time estimate
- Calculate material consumption
- Store G-code (optional)

**Acceptance Criteria:**
- Slicing completes within 60s for typical models
- Estimates within 20% of actual (before calibration)
- Supports all material/technology combinations

---

### FR-SCHED-003: Capability Filtering
**Priority:** Critical  
**Description:** Filter labs by hard constraints.

**Requirements:**
- Technology match
- Build volume sufficient
- Tolerance achievable
- Material in stock
- Machine operational
- Lab in good standing

**Acceptance Criteria:**
- Only feasible labs returned
- Filter executes in <1s
- 100% accurate (no infeasible assignments)

---

### FR-SCHED-004: Multi-Criteria Scoring
**Priority:** Critical  
**Description:** Score feasible assignments.

**Requirements:**
- Calculate scores for: deadline slack, current load, quality history, transfer time, internal cost
- Apply configurable weights
- Normalize across different units
- Log scores for audit

**Acceptance Criteria:**
- Scores range 0-1
- Higher score = better fit
- Weights sum to 1.0
- Transparent and explainable

---

### FR-SCHED-005: Quote-Time Scheduling
**Priority:** Critical  
**Description:** Run trial placement for quoting.

**Requirements:**
- Treat as speculative order
- Run full assignment and placement
- Return earliest feasible delivery date
- Calculate priority surcharge if urgent
- Async execution

**Acceptance Criteria:**
- Completes within 60s
- Delivery date achievable
- Does not affect real schedule
- Quote expiry accounts for staleness

---

### FR-SCHED-006: Job Placement
**Priority:** Critical  
**Description:** Assign confirmed orders to machines.

**Requirements:**
- Run capability filter and scoring
- Select top-scored lab/machine
- Calculate internal due date (backward from delivery)
- Check for batch consolidation opportunity
- Create job assignment record

**Acceptance Criteria:**
- Jobs assigned to feasible machines
- Internal due dates allow deadline
- Batching saves changeover time
- Assignment logged

---

### FR-SCHED-007: Event-Driven Rescheduling
**Priority:** Critical  
**Description:** Replan on production events.

**Requirements:**
- Trigger on: rejection, failure, breakdown, reprint, urgent insertion
- Identify affected jobs
- Generate dispatching rule fallback
- Improve within time budget (30s)
- Commit best solution found
- Notify stakeholders

**Acceptance Criteria:**
- Rescheduling completes within 30s
- Feasibility maintained
- In-progress jobs untouched
- Notifications sent

---

### FR-SCHED-008: Estimation Calibration
**Priority:** High  
**Description:** Learn from actual outcomes.

**Requirements:**
- Collect (estimated, actual) pairs
- Group by machine model
- Run regression per model
- Calculate correction factors
- Apply to future estimates
- Track MAPE over time

**Acceptance Criteria:**
- Calibration runs weekly
- Factors improve accuracy
- MAPE decreases over time
- Min 10 samples before applying

---

## 5. Analytics and Operations Module (FR-ANALYTICS)

### FR-ANALYTICS-001: Network Monitoring Dashboard
**Priority:** High  
**Description:** Real-time network visibility.

**Requirements:**
- Active orders count
- Jobs at risk (deadline within 4h)
- Current network load %
- Lab status summary (operational/at capacity/offline)
- Recent alerts

**Acceptance Criteria:**
- Updates every 30 seconds
- Drill-down to details
- Color-coded by urgency
- Exportable data

---

### FR-ANALYTICS-002: Lab Performance Tracking
**Priority:** High  
**Description:** Calculate and display lab metrics.

**Requirements:**
- On-time delivery rate
- First-pass yield
- Acceptance rate
- Utilization
- Performance score
- 90-day sliding window

**Acceptance Criteria:**
- Metrics accurate
- Updates daily
- Transparent calculation
- Historical trends visible

---

### FR-ANALYTICS-003: SLA Reporting
**Priority:** Medium  
**Description:** Generate compliance reports.

**Requirements:**
- Network-wide SLA metrics
- Per-lab breakdown
- Time period selection
- Export to PDF/Excel
- Trend analysis

**Acceptance Criteria:**
- Reports accurate and complete
- Generates within 10s
- Formatted professionally

---

### FR-ANALYTICS-004: Manual Intervention Tools
**Priority:** Medium  
**Description:** Operations manager overrides.

**Requirements:**
- Reassign job to different lab
- Adjust job priority
- Extend deadline (with customer notification)
- Suspend lab
- Cap lab capacity
- All with justification required

**Acceptance Criteria:**
- Interventions logged in audit trail
- Justification mandatory
- Feasibility still validated
- Stakeholders notified

---

## 6. Administration Module (FR-ADMIN)

### FR-ADMIN-001: User Management
**Priority:** High  
**Description:** Manage users and roles.

**Requirements:**
- Create/edit/disable users
- Assign roles (Customer, Lab Manager, Lab Operator, Hub Staff, Operations Manager, Admin)
- Reset passwords
- View login history
- Audit user actions

**Acceptance Criteria:**
- RBAC enforced
- Role changes immediate
- Disabled users cannot login

---

### FR-ADMIN-002: Configuration Management
**Priority:** High  
**Description:** Adjust business parameters.

**Requirements:**
- Pricing parameters (material rates, machine time, etc.)
- Assignment weights
- Scheduling limits (batch size, time budgets)
- SLA thresholds
- All versioned
- Audit trail

**Acceptance Criteria:**
- Changes create new version
- Old records use old version
- No code deployment required
- Validation prevents invalid values

---

### FR-ADMIN-003: Catalog Management
**Priority:** Medium  
**Description:** Maintain reference data.

**Requirements:**
- Materials (name, properties, rate)
- Colors (name, premium flag)
- Technologies (name, description)
- Quality grades (name, checklist)
- Post-processing services

**Acceptance Criteria:**
- CRUD operations functional
- Changes propagate to UI
- Cannot delete in-use items

---

### FR-ADMIN-004: Audit Log Viewer
**Priority:** Medium  
**Description:** Review system activity.

**Requirements:**
- Search by user, event type, date range
- View detailed event data
- Export logs
- Filter and sort

**Acceptance Criteria:**
- All sensitive actions logged
- Logs immutable
- Search performs <2s

---

## Summary

**Total Functional Requirements: 44**

| Module | Requirements | Critical | High | Medium |
|--------|-------------|----------|------|--------|
| Customer | 11 | 6 | 3 | 2 |
| Lab | 7 | 4 | 2 | 1 |
| Hub | 4 | 1 | 3 | 0 |
| Scheduling | 8 | 7 | 1 | 0 |
| Analytics | 4 | 2 | 1 | 1 |
| Admin | 4 | 2 | 1 | 1 |

All requirements are **testable**, **traceable to business rules**, and **aligned with system scope**.
