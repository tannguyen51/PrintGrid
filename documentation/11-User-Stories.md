# User Stories - PrintGrid Platform

## Overview
This document contains user stories for all platform personas, organized by Epic. Each story follows the format:
- **Story ID**
- **Title**
- **User Story:** As a [role], I want [goal], so that [benefit]
- **Acceptance Criteria**
- **Story Points** (Fibonacci: 1, 2, 3, 5, 8, 13)
- **Priority:** Critical / High / Medium / Low
- **Dependencies**

---

## Epic 1: Customer Order Management

### US-001: Upload 3D Model
**Title:** Upload 3D Model Files  
**User Story:** As a customer, I want to upload my 3D model files (STL/OBJ/3MF), so that I can get a quote for printing.

**Acceptance Criteria:**
- AC1: User can select and upload STL, OBJ, or 3MF files up to 50 MB
- AC2: Upload progress indicator shows percentage complete
- AC3: System validates file format and shows error for unsupported formats
- AC4: Successfully uploaded files appear in upload confirmation
- AC5: File stored securely in MinIO with unique identifier

**Story Points:** 5  
**Priority:** Critical  
**Dependencies:** None

---

### US-002: Preview 3D Model
**Title:** Interactive 3D Model Preview  
**User Story:** As a customer, I want to preview my uploaded model in 3D, so that I can verify it's correct before ordering.

**Acceptance Criteria:**
- AC1: Model renders in browser using Three.js
- AC2: User can rotate, zoom, and pan the model
- AC3: Bounding box dimensions displayed (X, Y, Z in mm)
- AC4: Toggle between solid and wireframe view
- AC5: Model statistics shown (vertices, faces)
- AC6: Preview loads within 5 seconds for typical models

**Story Points:** 8  
**Priority:** High  
**Dependencies:** US-001

---

### US-003: Validate Model Geometry
**Title:** Automatic Model Validation  
**User Story:** As a customer, I want the system to automatically check my model for issues, so that I know it's printable before ordering.

**Acceptance Criteria:**
- AC1: System checks mesh watertightness
- AC2: System detects non-manifold edges
- AC3: System validates build volume feasibility across network
- AC4: Validation results displayed within 10 seconds
- AC5: Specific issues listed with locations
- AC6: Auto-repair suggested for fixable issues

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-001

---

### US-004: Configure Print Settings
**Title:** Select Print Configuration  
**User Story:** As a customer, I want to configure print parameters (material, color, quality), so that I get exactly what I need.

**Acceptance Criteria:**
- AC1: Material dropdown shows available materials (PLA, PETG, ABS, etc.)
- AC2: Color selection updates based on material
- AC3: Quality grade options: Draft, Standard, High, Ultra
- AC4: Infill density selector: 10%, 30%, 50%, 100%
- AC5: Quantity input (1-100)
- AC6: Post-processing options checkboxes
- AC7: Unavailable combinations disabled with tooltip explanation
- AC8: Configuration saved with model

**Story Points:** 5  
**Priority:** Critical  
**Dependencies:** US-003

---

### US-005: Receive Instant Quote
**Title:** Get Price Quote and Delivery Date  
**User Story:** As a customer, I want to receive an instant quote with price and delivery date, so that I can decide whether to order.

**Acceptance Criteria:**
- AC1: Preliminary estimate shown within 5 seconds
- AC2: Full quote (with trial placement) completes within 60 seconds
- AC3: Price breakdown displayed: material, print time, post-processing, inspection, shipping
- AC4: Committed delivery date shown (from capacity-based scheduling)
- AC5: Quote expiry time displayed (48 hours)
- AC6: "Save Quote" button stores for later retrieval
- AC7: Priority/rush option shown if available with surcharge

**Story Points:** 13  
**Priority:** Critical  
**Dependencies:** US-004

---

### US-006: Place Order
**Title:** Confirm Order and Pay  
**User Story:** As a customer, I want to review my quote and complete payment, so that my order enters production.

**Acceptance Criteria:**
- AC1: Order summary shows all items, configuration, and pricing
- AC2: Delivery address form with validation
- AC3: Payment method selection (credit card via Stripe test mode)
- AC4: Terms and conditions acceptance checkbox
- AC5: "Place Order" button disabled until all required fields complete
- AC6: Payment processing with loading indicator
- AC7: Order confirmation page with order number
- AC8: Confirmation email sent to customer

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-005

---

### US-007: Track Order Status
**Title:** Real-Time Order Tracking  
**User Story:** As a customer, I want to track my order status in real-time, so that I know when to expect delivery.

**Acceptance Criteria:**
- AC1: Order status page shows current stage (7 stages)
- AC2: Progress bar or timeline visualization
- AC3: Estimated completion time updates dynamically
- AC4: Status descriptions user-friendly (no technical jargon)
- AC5: Email notifications on status change
- AC6: Real-time updates via SignalR (no page refresh needed)
- AC7: Lab identity NOT visible to customer
- AC8: Contact support button available

**Story Points:** 8  
**Priority:** High  
**Dependencies:** US-006

---

### US-008: Manage Model Library
**Title:** Personal Model Library  
**User Story:** As a customer, I want to save my uploaded models in a library, so that I can easily reorder them later.

**Acceptance Criteria:**
- AC1: Library page shows all customer's uploaded models
- AC2: Thumbnail previews generated for each model
- AC3: Model metadata displayed: name, dimensions, upload date
- AC4: Search and filter capabilities
- AC5: Delete model with confirmation dialog
- AC6: "Reorder" button pre-fills configuration from previous order
- AC7: Rename model functionality

**Story Points:** 5  
**Priority:** Medium  
**Dependencies:** US-001

---

### US-009: Request Reprint
**Title:** Quality Issue Reprint Request  
**User Story:** As a customer, I want to request a reprint if I receive a defective part, so that I get a quality product.

**Acceptance Criteria:**
- AC1: "Request Reprint" button on completed order
- AC2: Form to describe issue with predefined options
- AC3: Photo upload capability (multiple photos)
- AC4: Submit triggers review workflow
- AC5: Request status trackable
- AC6: Acknowledgment notification sent immediately
- AC7: Available within 30 days of delivery

**Story Points:** 5  
**Priority:** Medium  
**Dependencies:** US-007

---

## Epic 2: Lab Operations

### US-010: Register Lab
**Title:** Lab Registration  
**User Story:** As a lab owner, I want to register my lab on the platform, so that I can receive printing jobs.

**Acceptance Criteria:**
- AC1: Registration form captures: lab name, location, contact, business hours
- AC2: Distance/transfer time to hub input
- AC3: Bank account details for payments
- AC4: Business documentation upload (license, insurance)
- AC5: Submit triggers admin review workflow
- AC6: Email notification on approval/rejection

**Story Points:** 5  
**Priority:** High  
**Dependencies:** None

---

### US-011: Manage Machine Registry
**Title:** Add and Configure Machines  
**User Story:** As a lab manager, I want to register my 3D printers with specifications, so that appropriate jobs are assigned to me.

**Acceptance Criteria:**
- AC1: Add machine form: technology, brand, model, build volume (X/Y/Z), layer height range, tolerance
- AC2: Edit existing machine specifications
- AC3: Delete machine (blocked if active jobs)
- AC4: Mark machine status: operational, maintenance, offline
- AC5: Schedule planned maintenance windows
- AC6: View machine utilization statistics
- AC7: Changes reflect in assignment within 1 minute

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-010

---

### US-012: Track Material Inventory
**Title:** Material Inventory Management  
**User Story:** As a lab manager, I want to track material stock levels, so that I don't accept jobs I can't fulfill.

**Acceptance Criteria:**
- AC1: Add material: type, color, quantity (grams), reorder point
- AC2: Low stock alert when below reorder point
- AC3: Consumption auto-updated when job completed
- AC4: Restock entry with date and quantity
- AC5: Projected depletion date based on current queue
- AC6: Filter and search materials
- AC7: Inventory reflected in assignment within 1 minute

**Story Points:** 5  
**Priority:** Critical  
**Dependencies:** US-010

---

### US-013: Accept or Reject Job
**Title:** Job Assignment Review  
**User Story:** As a lab manager, I want to review assigned jobs before accepting, so that I can reject infeasible work.

**Acceptance Criteria:**
- AC1: Real-time notification when job assigned
- AC2: Job details page shows: model preview, configuration, estimated time, material required, internal due date
- AC3: Download model file button (time-limited access)
- AC4: Accept button commits to job
- AC5: Reject button requires reason selection
- AC6: Response deadline countdown visible (2 hours)
- AC7: Auto-reject if no response within deadline
- AC8: Acceptance/rejection logged in performance metrics

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-011, US-012

---

### US-014: Execute Production Workflow
**Title:** Lab Operator Job Management  
**User Story:** As a lab operator, I want to manage the production queue and update job status, so that the system knows progress.

**Acceptance Criteria:**
- AC1: View machine queue showing jobs in priority order
- AC2: Job detail view with all specifications
- AC3: Download model file for assigned job
- AC4: "Start Job" button marks as IN_PROGRESS
- AC5: "Report Completion" form captures actual print time and material used
- AC6: "Report Incident" button for failures with photo upload
- AC7: Status updates reflected system-wide immediately
- AC8: Mobile-responsive interface (tablet-optimized)
- AC9: Offline mode saves updates, syncs when connected

**Story Points:** 13  
**Priority:** Critical  
**Dependencies:** US-013

---

### US-015: View Schedule Timeline
**Title:** Machine Schedule Visualization  
**User Story:** As a lab manager, I want to see my machine schedules in a timeline, so that I can plan capacity.

**Acceptance Criteria:**
- AC1: Gantt chart per machine
- AC2: Jobs shown as bars with job ID and duration
- AC3: Color-coded by priority (normal, urgent, reprint)
- AC4: Click job for details popup
- AC5: Show internal due dates on hover
- AC6: Filter by machine or material type
- AC7: Total queue duration displayed
- AC8: Print/export schedule to PDF
- AC9: Updates after rescheduling

**Story Points:** 8  
**Priority:** Medium  
**Dependencies:** US-011

---

### US-016: Monitor Performance Metrics
**Title:** Lab Performance Dashboard  
**User Story:** As a lab manager, I want to see my performance metrics, so that I can identify improvement areas.

**Acceptance Criteria:**
- AC1: Overall performance score displayed prominently
- AC2: Metrics shown: on-time delivery rate, first-pass yield, acceptance rate, utilization
- AC3: Trend charts over time (last 90 days)
- AC4: Comparison to network average (anonymized)
- AC5: Defect breakdown by type
- AC6: Revenue summary for current month
- AC7: Top issues/recommendations section
- AC8: Updates daily

**Story Points:** 8  
**Priority:** Medium  
**Dependencies:** US-010

---

## Epic 3: Hub Quality Control

### US-017: Receive Lab Shipments
**Title:** Batch Receipt and Reconciliation  
**User Story:** As hub staff, I want to receive and reconcile incoming batches from labs, so that I know what to inspect.

**Acceptance Criteria:**
- AC1: Scan batch barcode from lab
- AC2: System displays expected jobs in batch
- AC3: Scan individual job QR codes
- AC4: Reconciliation summary: received vs. expected
- AC5: Flag discrepancies (missing items)
- AC6: Automatic notification to lab for discrepancies
- AC7: Move received items to inspection queue

**Story Points:** 5  
**Priority:** High  
**Dependencies:** US-014

---

### US-018: Inspect Part Quality
**Title:** Checklist-Driven Quality Inspection  
**User Story:** As QC inspector, I want to inspect parts using a standardized checklist, so that quality is consistent.

**Acceptance Criteria:**
- AC1: Select job from inspection queue
- AC2: Load checklist based on quality grade ordered
- AC3: Step through checks with pass/fail/N/A options
- AC4: Mandatory photo capture from multiple angles
- AC5: Overall pass/fail decision
- AC6: If FAIL: defect classification dropdown
- AC7: If FAIL: fault attribution (lab/hub/customer)
- AC8: Complete inspection within 5 minutes for standard parts
- AC9: Decision saved and triggers appropriate action

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-017

---

### US-019: Manage Reprints
**Title:** Reprint Initiation and Tracking  
**User Story:** As QC inspector, I want to automatically trigger reprints for failed parts, so that customers get quality products.

**Acceptance Criteria:**
- AC1: Lab fault → automatic reprint job created with URGENT priority
- AC2: Reprint inherits original deadline
- AC3: Customer fault → notification to customer, no auto-reprint
- AC4: Hub fault → reprint at platform expense
- AC5: Reprint status trackable
- AC6: Original job marked as "reprinting"
- AC7: Customer notified appropriately based on fault
- AC8: Reprint limit enforced (escalate after 2 reprints)

**Story Points:** 8  
**Priority:** High  
**Dependencies:** US-018

---

### US-020: Consolidate and Ship Orders
**Title:** Order Fulfillment  
**User Story:** As hub fulfillment staff, I want to consolidate all items for an order and ship, so that customers receive complete orders.

**Acceptance Criteria:**
- AC1: View orders ready for packing (all items passed inspection)
- AC2: Print packing slip
- AC3: Package items with protective material
- AC4: Record package dimensions and weight
- AC5: Generate shipping label
- AC6: Scan package to carrier
- AC7: Update order status to "shipped"
- AC8: Customer receives tracking notification

**Story Points:** 5  
**Priority:** High  
**Dependencies:** US-018

---

## Epic 4: Scheduling and Assignment

### US-021: Analyze Uploaded Models
**Title:** Geometry Analysis Service  
**User Story:** As the system, I want to analyze uploaded models automatically, so that I have data for scheduling.

**Acceptance Criteria:**
- AC1: Parse STL/OBJ/3MF file structure
- AC2: Calculate bounding box (X, Y, Z) accurate to 0.1mm
- AC3: Compute volume
- AC4: Check mesh watertightness
- AC5: Detect non-manifold edges
- AC6: Analysis completes within 10 seconds
- AC7: Results stored with model record

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-001

---

### US-022: Generate Print Estimates
**Title:** Slicing and Estimation Service  
**User Story:** As the system, I want to slice models and estimate print time, so that I can quote accurately.

**Acceptance Criteria:**
- AC1: Integrate CuraEngine or PrusaSlicer CLI
- AC2: Map configuration to slicer profile
- AC3: Execute slicing asynchronously
- AC4: Extract print time estimate from G-code
- AC5: Calculate material consumption (primary + support)
- AC6: Slicing completes within 60 seconds for typical models
- AC7: Store G-code (optional) or estimates with model
- AC8: Handle slicing errors gracefully

**Story Points:** 13  
**Priority:** Critical  
**Dependencies:** US-021

---

### US-023: Filter by Capability
**Title:** Hard Constraint Filtering  
**User Story:** As the system, I want to filter labs by capability constraints, so that I only assign feasible jobs.

**Acceptance Criteria:**
- AC1: Filter by technology match
- AC2: Filter by build volume sufficient
- AC3: Filter by tolerance achievable
- AC4: Filter by material in stock
- AC5: Filter by machine operational
- AC6: Filter by lab in good standing
- AC7: Return only labs passing ALL filters
- AC8: Filter executes in <1 second
- AC9: Log filtered candidates

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-011, US-012

---

### US-024: Score Assignment Candidates
**Title:** Multi-Criteria Assignment Scoring  
**User Story:** As the system, I want to score feasible labs using multiple criteria, so that I assign optimally.

**Acceptance Criteria:**
- AC1: Calculate deadline slack score
- AC2: Calculate current load score
- AC3: Calculate quality history score
- AC4: Calculate transfer time score
- AC5: Calculate internal cost score
- AC6: Apply configurable weights
- AC7: Normalize scores to 0-1 range
- AC8: Composite score calculated
- AC9: Log all scores for audit
- AC10: Scoring completes in <1 second

**Story Points:** 8  
**Priority:** Critical  
**Dependencies:** US-023

---

### US-025: Run Quote-Time Scheduling
**Title:** Speculative Trial Placement  
**User Story:** As the system, I want to run trial placement at quote time, so that I can promise realistic delivery dates.

**Acceptance Criteria:**
- AC1: Treat as speculative order (doesn't commit)
- AC2: Run full capability filter and scoring
- AC3: Place on machine schedule (trial)
- AC4: Calculate backward from available slots
- AC5: Return earliest feasible delivery date
- AC6: Calculate priority surcharge if urgent
- AC7: Complete within 60 seconds
- AC8: Does not affect real schedule
- AC9: Results cached with quote expiry

**Story Points:** 13  
**Priority:** Critical  
**Dependencies:** US-024

---

### US-026: Assign Jobs to Machines
**Title:** Job Assignment and Placement  
**User Story:** As the system, I want to assign confirmed jobs to specific machines, so that production can start.

**Acceptance Criteria:**
- AC1: Run capability filter and scoring
- AC2: Select top-scored lab/machine
- AC3: Calculate internal due date (backward from delivery)
- AC4: Check for batch consolidation opportunity
- AC5: Place on machine schedule
- AC6: Create job assignment record
- AC7: Notify assigned lab
- AC8: Update machine capacity
- AC9: Log assignment decision with scores

**Story Points:** 13  
**Priority:** Critical  
**Dependencies:** US-025

---

### US-027: Reschedule on Events
**Title:** Event-Driven Rescheduling  
**User Story:** As the system, I want to reschedule automatically when failures occur, so that deadlines are still met.

**Acceptance Criteria:**
- AC1: Trigger on: rejection, print failure, machine breakdown, reprint, urgent order
- AC2: Identify affected jobs
- AC3: Generate dispatching rule fallback solution
- AC4: Improve solution within 30-second time budget
- AC5: Commit best solution found
- AC6: In-progress jobs NOT modified
- AC7: Customer deadlines NOT silently changed
- AC8: Notify affected labs and customers
- AC9: Log rescheduling event and results

**Story Points:** 13  
**Priority:** Critical  
**Dependencies:** US-026

---

### US-028: Calibrate Estimates
**Title:** Print Time Calibration Loop  
**User Story:** As the system, I want to calibrate print time estimates from actual data, so that estimates improve over time.

**Acceptance Criteria:**
- AC1: Collect (estimated, actual) pairs from completed jobs
- AC2: Group by machine model
- AC3: Run linear regression per model
- AC4: Calculate correction factors
- AC5: Apply factors to future estimates
- AC6: Track MAPE over time
- AC7: Calibration runs weekly
- AC8: Minimum 10 samples before applying
- AC9: Outliers filtered (>3 std dev)

**Story Points:** 8  
**Priority:** High  
**Dependencies:** US-014

---

## Epic 5: Platform Administration

### US-029: Manage Users and Roles
**Title:** User and Role Management  
**User Story:** As an admin, I want to manage users and assign roles, so that access is controlled.

**Acceptance Criteria:**
- AC1: List all users with search and filter
- AC2: Create new user with role assignment
- AC3: Edit user details
- AC4: Disable/enable user accounts
- AC5: Reset user passwords
- AC6: View login history per user
- AC7: Audit log of admin actions
- AC8: Role changes take effect immediately

**Story Points:** 5  
**Priority:** High  
**Dependencies:** None

---

### US-030: Configure Business Parameters
**Title:** Configuration Management  
**User Story:** As an admin, I want to adjust pricing and scheduling parameters without code deployment, so that I can tune the system.

**Acceptance Criteria:**
- AC1: Edit pricing parameters: material rates, machine time rates, etc.
- AC2: Edit assignment weights (5 criteria)
- AC3: Edit scheduling limits: batch size, time budgets, etc.
- AC4: Edit SLA thresholds
- AC5: Changes create new version (not modify existing)
- AC6: Preview impact before saving
- AC7: Validation prevents invalid values (e.g., weights sum to 1.0)
- AC8: Audit trail of all changes

**Story Points:** 8  
**Priority:** High  
**Dependencies:** None

---

### US-031: Manage Catalogs
**Title:** Reference Data Management  
**User Story:** As an admin, I want to manage catalogs of materials, colors, and services, so that they stay up-to-date.

**Acceptance Criteria:**
- AC1: Materials: add, edit, delete (name, properties, default rate)
- AC2: Colors: add, edit, delete (name, premium flag)
- AC3: Technologies: add, edit, delete (name, description)
- AC4: Quality grades: add, edit, delete (name, checklist)
- AC5: Post-processing services: add, edit
- AC6: Cannot delete items in active use
- AC7: Changes propagate to UI immediately

**Story Points:** 5  
**Priority:** Medium  
**Dependencies:** None

---

### US-032: View Audit Logs
**Title:** System Audit Log  
**User Story:** As an admin, I want to view complete audit logs, so that I can investigate issues and ensure compliance.

**Acceptance Criteria:**
- AC1: Search by user, event type, date range
- AC2: View detailed event data (before/after values)
- AC3: Filter and sort results
- AC4: Export to CSV/Excel
- AC5: Logs immutable (no deletion)
- AC6: Search results within 2 seconds
- AC7: Sensitive events highlighted (payment, access control changes)

**Story Points:** 5  
**Priority:** Medium  
**Dependencies:** None

---

## Epic 6: Monitoring and Operations

### US-033: Monitor Network Status
**Title:** Real-Time Network Dashboard  
**User Story:** As an operations manager, I want to see real-time network status, so that I can identify issues quickly.

**Acceptance Criteria:**
- AC1: Active orders count
- AC2: Jobs at risk (deadline within 4 hours) highlighted
- AC3: Current network load percentage
- AC4: Lab status summary (operational/at capacity/offline)
- AC5: Recent alerts feed
- AC6: Updates every 30 seconds
- AC7: Drill-down to details on click
- AC8: Color-coded by urgency

**Story Points:** 8  
**Priority:** High  
**Dependencies:** US-026

---

### US-034: Manually Intervene
**Title:** Manual Override Tools  
**User Story:** As an operations manager, I want to manually override system decisions when needed, so that I can handle exceptions.

**Acceptance Criteria:**
- AC1: Reassign job to different lab (with justification)
- AC2: Adjust job priority (with justification)
- AC3: Extend delivery deadline (requires customer notification)
- AC4: Suspend lab (with reason and duration)
- AC5: Cap lab capacity (set max concurrent jobs)
- AC6: All interventions logged in audit trail
- AC7: Feasibility still validated
- AC8: Affected parties notified

**Story Points:** 8  
**Priority:** Medium  
**Dependencies:** US-033

---

### US-035: Generate SLA Reports
**Title:** SLA Reporting  
**User Story:** As an operations manager, I want to generate SLA compliance reports, so that I can track performance.

**Acceptance Criteria:**
- AC1: Network-wide SLA metrics calculated
- AC2: Per-lab breakdown available
- AC3: Time period selection (weekly, monthly, quarterly)
- AC4: Metrics: on-time rate, defect rate, etc.
- AC5: Trend analysis charts
- AC6: Export to PDF and Excel
- AC7: Report generates within 10 seconds
- AC8: Professional formatting

**Story Points:** 8  
**Priority:** Medium  
**Dependencies:** US-033

---

## Summary

**Total User Stories: 35**

| Epic | Stories | Total SP | Priority Distribution |
|------|---------|----------|----------------------|
| Customer Order Management | 9 | 70 | Critical: 5, High: 2, Medium: 2 |
| Lab Operations | 7 | 55 | Critical: 4, Medium: 3 |
| Hub Quality Control | 4 | 26 | Critical: 1, High: 3 |
| Scheduling and Assignment | 8 | 82 | Critical: 6, High: 2 |
| Platform Administration | 4 | 23 | High: 2, Medium: 2 |
| Monitoring and Operations | 3 | 24 | High: 1, Medium: 2 |

**Total Story Points: 280**  
**Average Velocity (assuming 2-week sprints):** 35 SP/sprint  
**Estimated Sprints:** 8 sprints (~16 weeks for MVP)

**Sprint Planning Recommendation:**
- Sprint 1-2: US-001 to US-006 (Customer core flow)
- Sprint 3-4: US-021 to US-027 (Scheduling engine - CORE)
- Sprint 5-6: US-010 to US-016 (Lab operations)
- Sprint 7-8: US-017 to US-020, US-029 to US-035 (Hub + Admin)

**Critical Path:** US-001 → US-021 → US-022 → US-023 → US-024 → US-025 → US-026 → US-027
