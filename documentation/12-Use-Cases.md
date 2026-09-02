# Use Cases - PrintGrid Platform

## Overview
This document details the use cases for PrintGrid platform, describing interactions between actors and the system. Each use case includes:
- Use Case ID, Name, and Description
- Actors (Primary and Secondary)
- Preconditions and Postconditions
- Main Flow (Happy Path)
- Alternate Flows (Exceptions and Variations)
- Business Rules Referenced

---

## UC-001: Place Order for 3D Printing

**Description:** Customer uploads a 3D model, configures print settings, receives a quote, and places an order.

**Actors:**
- Primary: Customer
- Secondary: Scheduling System, Payment Gateway

**Preconditions:**
- Customer has registered account
- Customer is authenticated

**Postconditions:**
- Order created in system
- Payment processed
- Jobs assigned to labs
- Customer receives confirmation

**Main Flow:**
1. Customer navigates to "Upload Model" page
2. Customer selects and uploads 3D model file (STL/OBJ/3MF)
3. System validates file format and size
4. System analyzes model geometry (UC-002)
5. System displays model preview in 3D viewer
6. Customer reviews validation results
7. Customer configures print settings:
   - Selects material (e.g., PLA)
   - Selects color
   - Selects quality grade (Standard)
   - Sets infill density (30%)
   - Sets quantity (1)
   - Optionally adds post-processing
8. Customer clicks "Get Quote"
9. System performs slicing analysis (async)
10. System shows preliminary estimate immediately
11. System runs speculative quote-time scheduling (UC-010)
12. System displays full quote with:
    - Total price with breakdown
    - Committed delivery date
    - Quote expiry time (48 hours)
13. Customer reviews quote
14. Customer clicks "Place Order"
15. Customer enters/confirms delivery address
16. Customer selects payment method
17. Customer accepts terms and conditions
18. Customer clicks "Confirm and Pay"
19. System processes payment via payment gateway
20. System confirms payment successful
21. System creates order record
22. System triggers job assignment (UC-011)
23. System sends confirmation email to customer
24. System displays order confirmation page with order number

**Alternate Flows:**

**A1: Invalid File Format (Step 3)**
- 3a. System detects unsupported format
- 3b. System displays error: "File format not supported. Please upload STL, OBJ, or 3MF"
- 3c. Return to step 2

**A2: Model Validation Fails (Step 6)**
- 6a. System detects geometry issues (non-manifold, not watertight)
- 6b. System lists specific issues with locations
- 6c. System offers auto-repair if possible
- 6d. If customer accepts repair: system repairs and continues to step 7
- 6e. If customer declines: return to step 2 to upload different model

**A3: Model Exceeds Build Volume (Step 6)**
- 6a. System checks model against network capacity
- 6b. No lab has sufficient build volume
- 6c. System displays error: "Model exceeds maximum build volume of network (X×Y×Z mm). Please scale down or split model."
- 6d. Cannot continue to quote

**A4: Quote Expired (Step 13)**
- 13a. Customer returns to quote after 48 hours
- 13b. System shows "Quote Expired" message
- 13c. Offer "Get New Quote" button
- 13d. If clicked, return to step 9 with same configuration

**A5: Payment Fails (Step 19)**
- 19a. Payment gateway returns error
- 19b. System displays error message with reason
- 19c. Offer "Try Again" or "Change Payment Method"
- 19d. If retry: return to step 19
- 19e. If change method: return to step 16

**Business Rules:**
- BR-QUOTE-001: Network-wide price uniformity
- BR-QUOTE-002: Capacity-based delivery dates
- BR-QUOTE-003: Quote expiry time
- BR-QUOTE-006: Infeasible part rejection
- BR-ASSIGN-001: Hard constraint filtering
- BR-PAY-001: Payment on order confirmation

---

## UC-002: Analyze Model Geometry

**Description:** System automatically analyzes uploaded 3D model to extract scheduling inputs and validate printability.

**Actors:**
- Primary: System
- Secondary: Slicing Service

**Preconditions:**
- Valid 3D model file uploaded

**Postconditions:**
- Model analyzed and results stored
- Bounding box calculated
- Validation status determined

**Main Flow:**
1. System receives uploaded model file from MinIO
2. System parses file format (STL/OBJ/3MF)
3. System extracts mesh data (vertices, faces)
4. System calculates bounding box (X, Y, Z dimensions)
5. System computes volume
6. System checks mesh watertightness
7. System detects non-manifold edges
8. System validates against network max build volume
9. System stores analysis results with model record
10. System returns validation status to caller

**Alternate Flows:**

**A1: Corrupted File (Step 2)**
- 2a. Parser cannot read file structure
- 2b. Return error: "File corrupted or invalid"

**A2: Exceeds Network Capacity (Step 8)**
- 8a. Bounding box exceeds all machines in network
- 8b. Set validation status: INFEASIBLE
- 8c. Store reason: "Exceeds max build volume"

**Business Rules:**
- BR-QUOTE-006: Infeasible part rejection

---

## UC-003: Track Order

**Description:** Customer views real-time status of their order.

**Actors:**
- Primary: Customer

**Preconditions:**
- Customer authenticated
- Order exists for customer

**Postconditions:**
- Customer informed of current status

**Main Flow:**
1. Customer navigates to "My Orders"
2. System displays list of customer's orders
3. Customer clicks on order to view details
4. System retrieves order status
5. System displays:
   - Order number and date
   - Items with thumbnails
   - Current status stage (e.g., "In Production")
   - Progress bar showing stages
   - Estimated completion time
   - Delivery date
6. System updates status in real-time via SignalR
7. Customer can click "Contact Support" if needed

**Alternate Flows:**

**A1: Order Delayed**
- 6a. System detects order behind schedule
- 6b. Estimated completion time updated
- 6c. "Delayed" badge shown
- 6d. Notification sent to customer

**Business Rules:**
- BR-NOTIFY-001: Critical events require notification

---

## UC-004: Lab Accepts Job Assignment

**Description:** Lab receives job assignment, reviews details, and accepts or rejects.

**Actors:**
- Primary: Lab Manager
- Secondary: Lab Operator

**Preconditions:**
- Lab registered and approved
- Lab has machines registered
- Job assigned to lab by scheduling system

**Postconditions:**
- Job acceptance/rejection recorded
- If accepted: job enters lab's queue
- If rejected: job reassigned by system

**Main Flow:**
1. System assigns job to lab (via UC-011)
2. System sends real-time notification to lab
3. Lab manager receives notification
4. Lab manager opens job assignment details
5. System displays:
   - Model preview
   - Print configuration
   - Estimated print time
   - Material required (type, color, quantity)
   - Internal due date
   - Download model button
6. Lab manager reviews capacity and materials
7. Lab manager clicks "Accept Job"
8. System records acceptance
9. System updates job status to ACCEPTED
10. System adds job to machine queue
11. System updates lab capacity
12. System sends confirmation to lab
13. System provides time-limited model file access

**Alternate Flows:**

**A1: Lab Rejects Job (Step 7)**
- 7a. Lab manager clicks "Reject Job"
- 7b. System prompts for reason:
  - Material out of stock
  - Machine unavailable
  - Infeasibility discovered
  - Capacity overbooked
- 7c. Lab manager selects reason and submits
- 7d. System records rejection
- 7e. System updates lab acceptance rate metric
- 7f. System triggers rescheduling (UC-012)

**A2: No Response Within Deadline (After Step 3)**
- 3a. 2 hours pass without response
- 3b. System auto-rejects job
- 3c. System records timeout
- 3d. System triggers rescheduling (UC-012)

**Business Rules:**
- BR-ASSIGN-005: Lab acceptance window
- BR-ASSIGN-006: Rejection requires justification
- BR-ACCESS-001: Time-limited model access
- BR-ACCESS-002: Access logging mandatory

---

## UC-005: Execute Print Job

**Description:** Lab operator executes printing job and reports completion.

**Actors:**
- Primary: Lab Operator

**Preconditions:**
- Job accepted by lab
- Job in machine queue
- Materials available

**Postconditions:**
- Job completed
- Actual time and materials recorded
- Part ready for handover

**Main Flow:**
1. Lab operator views machine queue on tablet
2. System displays jobs in priority order
3. Operator selects next job
4. System displays job details and instructions
5. Operator clicks "Download Model"
6. System provides time-limited download URL
7. Operator downloads STL file
8. Operator prepares machine:
   - Loads material
   - Calibrates bed
   - Imports file to slicer
   - Starts print
9. Operator clicks "Start Job" in system
10. System updates job status to IN_PROGRESS
11. Operator monitors print
12. Print completes successfully
13. Operator performs post-processing (support removal)
14. Operator clicks "Report Completion"
15. System prompts for:
    - Actual print duration
    - Actual material consumed
16. Operator enters data and confirms
17. System records actual values
18. System updates job status to COMPLETED
19. System triggers calibration data collection
20. System updates machine queue (remove job)
21. System notifies hub of completed part
22. Operator packages part and ships to hub

**Alternate Flows:**

**A1: Print Fails (Step 12)**
- 12a. Print fails (layer separation, warping, etc.)
- 12b. Operator clicks "Report Incident"
- 12c. System prompts for:
  - Incident type: Print Failure
  - Stage: percentage complete when failed
  - Description
  - Photos
- 12d. Operator uploads photos and submits
- 12e. System records incident
- 12f. System triggers rescheduling (UC-012) for reprint
- 12g. Operations manager notified

**A2: Machine Breaks Down (During Step 11)**
- 11a. Machine fails during print
- 11b. Operator clicks "Report Incident"
- 11c. System prompts for incident type: Machine Breakdown
- 11d. Operator marks machine as OFFLINE
- 11e. System triggers rescheduling (UC-012) for all jobs in machine queue
- 11f. Maintenance workflow initiated

**Business Rules:**
- BR-SCHED-007: In-progress jobs untouched (for rescheduling)
- BR-ESTIM-001: Actual time reporting required
- BR-ACCESS-001: Time-limited model access

---

## UC-006: Inspect Part Quality

**Description:** Hub QC staff inspect completed parts against quality checklist.

**Actors:**
- Primary: QC Inspector

**Preconditions:**
- Part received at hub from lab
- Part reconciled in batch receipt

**Postconditions:**
- Part inspection completed
- Pass/fail decision recorded
- If fail: fault attributed and reprint triggered

**Main Flow:**
1. QC inspector selects job from inspection queue
2. System loads inspection checklist for job's quality grade
3. System displays:
   - Job details
   - Part specifications
   - Quality checklist
4. Inspector retrieves physical part
5. Inspector steps through checklist:
   - Dimensional accuracy within tolerance
   - No visible defects (warping, layer separation)
   - Correct material and color
   - Support material removed
   - Surface finish meets standard
6. Inspector marks each check: PASS / FAIL / N/A
7. Inspector captures photos from multiple angles
8. Inspector uploads photos
9. All checks pass
10. Inspector clicks "Pass Inspection"
11. System records PASS result
12. System updates job status to PASSED_QC
13. System moves part to fulfillment queue
14. System updates lab's first-pass yield metric

**Alternate Flows:**

**A1: Part Fails Inspection (Step 9)**
- 9a. One or more checks fail
- 9b. System prompts for defect classification:
  - Dimensional inaccuracy
  - Surface defect
  - Wrong material/color
  - Incomplete post-processing
  - Physical damage
- 9c. Inspector selects defect type
- 9d. System prompts for fault attribution
- 9e. Inspector reviews defect and determines fault:
  - Lab (production issue)
  - Hub (handling damage)
  - Customer (geometry issue)
- 9f. Inspector selects fault party
- 9g. Inspector clicks "Fail Inspection"
- 9h. System records FAIL result with fault attribution
- 9i. System updates lab performance metrics (if lab fault)
- 9j. If lab or hub fault: System creates reprint job (UC-013)
- 9k. If customer fault: System notifies customer (no auto-reprint)

**Business Rules:**
- BR-QC-001: Central hub inspection mandatory
- BR-QC-002: Quality grade checklist binding
- BR-QC-003: Photographic evidence required
- BR-QC-004: Fault attribution required on failure
- BR-QC-005: Lab fault triggers reprint at lab expense
- BR-QC-006: Customer fault notification

---

## UC-007: Customer Requests Reprint

**Description:** Customer receives defective part and requests reprint.

**Actors:**
- Primary: Customer
- Secondary: Operations Manager

**Preconditions:**
- Order delivered to customer
- Within 30-day quality guarantee window

**Postconditions:**
- Reprint request logged
- Request reviewed by operations
- If approved: reprint job created

**Main Flow:**
1. Customer opens completed order
2. Customer clicks "Request Reprint"
3. System checks if within guarantee window (30 days)
4. System displays reprint request form
5. Customer describes issue
6. Customer uploads photos of defect
7. Customer submits request
8. System creates reprint request record
9. System notifies operations manager
10. System sends acknowledgment to customer
11. Operations manager reviews request and photos
12. Defect confirmed as platform responsibility
13. Operations manager approves reprint
14. System creates reprint job with priority URGENT
15. System inherits original deadline
16. System triggers job assignment (UC-011)
17. Customer notified of approval and new timeline

**Alternate Flows:**

**A1: Outside Guarantee Window (Step 3)**
- 3a. More than 30 days since delivery
- 3b. System displays message: "Quality guarantee period expired"
- 3c. Cannot submit request

**A2: Request Denied (Step 13)**
- 13a. Operations manager determines not platform fault (e.g., user damage)
- 13b. Operations manager denies request with explanation
- 13c. Customer notified of denial

**Business Rules:**
- BR-QC-006: Customer fault notification
- BR-RESCHED-003: Priority reprint handling

---

## UC-008: Lab Manager Views Performance

**Description:** Lab manager reviews performance metrics and identifies improvement areas.

**Actors:**
- Primary: Lab Manager

**Preconditions:**
- Lab registered and has completed jobs

**Postconditions:**
- Lab manager informed of performance

**Main Flow:**
1. Lab manager navigates to "Performance Dashboard"
2. System calculates metrics (90-day sliding window)
3. System displays:
   - Overall performance score (0.0 - 1.0)
   - Standing tier (Excellent/Good/Acceptable/Poor)
   - On-time delivery rate (%)
   - First-pass yield (%)
   - Acceptance rate (%)
   - Average utilization (%)
   - Revenue this month
4. System displays trend charts over time
5. System shows comparison to network average (anonymized)
6. System displays defect breakdown by type
7. System shows top issues and recommendations
8. Lab manager reviews metrics
9. Lab manager identifies improvement areas

**Alternate Flows:**

**A1: Insufficient Data (Step 2)**
- 2a. Lab has completed <10 jobs
- 2b. System displays message: "Insufficient data for scoring"
- 2c. Show preliminary metrics without composite score

**A2: Poor Performance (Step 3)**
- 3a. Performance score < 0.50
- 3b. System displays warning: "Performance Below Acceptable"
- 3c. System shows review notice if sustained for 30 days

**Business Rules:**
- BR-PERF-001: Performance metrics continuous tracking
- BR-PERF-002: Minimum sample size for scoring
- BR-PERF-004: Performance transparency to labs
- BR-PERF-005: Lab suspension on poor performance

---

## UC-009: Admin Configures Pricing

**Description:** Administrator adjusts pricing parameters without code deployment.

**Actors:**
- Primary: Platform Administrator

**Preconditions:**
- Admin authenticated with appropriate role

**Postconditions:**
- New pricing parameter version created
- Future quotes use new version
- Existing quotes unchanged

**Main Flow:**
1. Admin navigates to "Configuration > Pricing"
2. System displays current pricing parameters:
   - Material rates (per gram per type)
   - Machine time rates (per hour per technology)
   - Post-processing costs
   - Hub handling costs
   - Shipping rates
3. Admin clicks "Edit Parameters"
4. System loads editable form with current values
5. Admin modifies values (e.g., PLA rate from $0.32 to $0.35/gram)
6. System validates inputs (positive numbers, reasonable ranges)
7. Admin clicks "Preview Impact"
8. System shows sample quote before/after comparison
9. Admin reviews and clicks "Save as New Version"
10. System creates new parameter version record
11. System stores admin ID and timestamp
12. System writes to audit log
13. System displays success message
14. Future quotes now use new version
15. System displays version history

**Alternate Flows:**

**A1: Validation Fails (Step 6)**
- 6a. Invalid value entered (e.g., negative number)
- 6b. System displays validation error
- 6c. Return to step 5

**Business Rules:**
- BR-CONFIG-001: Parameter changes create new version
- BR-CONFIG-002: Configuration audit trail
- BR-CONFIG-003: No retroactive application

---

## UC-010: System Runs Speculative Scheduling

**Description:** System performs trial placement at quote time to determine realistic delivery date.

**Actors:**
- Primary: Scheduling System

**Preconditions:**
- Model sliced and estimated
- Print configuration selected

**Postconditions:**
- Earliest feasible delivery date determined
- Priority surcharge calculated if applicable

**Main Flow:**
1. System receives quote request
2. System retrieves current network state (labs, machines, queues)
3. System creates temporary/speculative order
4. System runs capability filtering (UC-011 steps 2-3)
5. System runs multi-criteria scoring (UC-011 steps 4-5)
6. System selects top-scored lab/machine
7. System calculates internal due date (backward from today + N days)
8. System checks machine schedule for earliest slot
9. System considers batch consolidation if applicable
10. System places job in trial schedule (not committed)
11. System calculates delivery date = slot_end + transfer_time + inspection_time + shipping_time
12. System checks if delivery date meets customer requirement
13. Delivery date feasible
14. System calculates deadline slack
15. If slack < urgent threshold: calculate priority surcharge
16. System stores trial placement results with quote
17. System returns delivery date and price to caller

**Alternate Flows:**

**A1: No Feasible Lab (Step 6)**
- 6a. Capability filter returns empty set
- 6b. Cannot place job
- 6c. Return error: "No lab can fulfill this order"

**A2: Delivery Date Not Achievable (Step 13)**
- 13a. Earliest delivery > customer required date
- 13b. Inform customer: "Cannot meet required date"
- 13c. Offer earliest feasible date as alternative

**Business Rules:**
- BR-QUOTE-002: Capacity-based delivery dates
- BR-QUOTE-007: Asynchronous quote processing
- BR-SCHED-001: Backward deadline scheduling

---

## UC-011: System Assigns Job to Lab

**Description:** System assigns confirmed order job to specific lab/machine.

**Actors:**
- Primary: Scheduling System

**Preconditions:**
- Order confirmed and paid
- Order decomposed into jobs

**Postconditions:**
- Job assigned to lab and machine
- Lab notified
- Assignment logged

**Main Flow:**
1. System receives job assignment request
2. System runs capability filtering:
   - Technology matches
   - Build volume sufficient
   - Tolerance achievable
   - Material in stock
   - Machine operational
   - Lab in good standing
3. System gets list of feasible (lab, machine) pairs
4. System runs multi-criteria scoring for each pair:
   - Calculate deadline slack
   - Calculate current load
   - Retrieve quality history
   - Calculate transfer time
   - Estimate internal cost
   - Normalize and weight
5. System sorts by composite score (descending)
6. System checks exploration allocation (5% random)
7. Not exploration case: select top-scored pair
8. System calculates internal due date:
   - delivery_date - shipping - inspection - transfer = lab_handover
   - lab_handover - print_time - post_process = start_date
9. System checks machine queue for batch consolidation opportunity
10. Same material/color batch exists and not full
11. System adds job to batch
12. System creates job assignment record
13. System logs assignment decision with all scores
14. System updates machine capacity
15. System sends notification to assigned lab
16. Lab has 2 hours to respond

**Alternate Flows:**

**A1: No Feasible Labs (Step 3)**
- 3a. Capability filter returns empty
- 3b. Escalate to operations manager
- 3c. Manual intervention required

**A2: Exploration Allocation (Step 7)**
- 7a. Random number < 0.05 (5% exploration)
- 7b. Select randomly from top 5 feasible labs
- 7c. Continue to step 8

**A3: Urgent Job - No Batching (Step 10)**
- 10a. Job deadline urgent (internal_due < 48h from now)
- 10b. Skip batch consolidation
- 10c. Place individually at earliest slot

**Business Rules:**
- BR-ASSIGN-001: Hard constraint filtering
- BR-ASSIGN-002: Multi-criteria scoring
- BR-ASSIGN-004: Assignment decision logging
- BR-ASSIGN-007: Exploration allocation for fairness
- BR-SCHED-001: Backward deadline scheduling
- BR-SCHED-002: Material/color batch consolidation
- BR-SCHED-004: Urgent job bypass

---

## UC-012: System Reschedules on Event

**Description:** System automatically replans when production events invalidate current schedule.

**Actors:**
- Primary: Scheduling System
- Secondary: Operations Manager (for notifications)

**Preconditions:**
- Production event occurs (rejection, failure, breakdown, reprint)

**Postconditions:**
- Schedule repaired
- Affected jobs reassigned
- Stakeholders notified

**Main Flow:**
1. System receives production event (e.g., print failure)
2. System identifies event type and affected jobs
3. Event is print failure for JOB-X
4. System retrieves job details
5. System removes JOB-X from current schedule
6. System identifies other affected jobs (same machine queue)
7. System generates dispatching-rule fallback solution:
   - Earliest Due Date rule
   - Ensures feasibility
8. System stores fallback as best_solution
9. System starts improvement search (30-second time budget)
10. For remaining time:
    - Try swap or transfer neighborhood moves
    - If improvement found: update best_solution
11. Time budget expires
12. System commits best_solution found
13. System updates machine schedules
14. System checks if any customer deadlines affected
15. No customer deadlines changed
16. System creates reprint job for JOB-X with priority URGENT
17. System assigns reprint job (UC-011)
18. System notifies affected labs of schedule changes
19. System logs rescheduling event and results

**Alternate Flows:**

**A1: Customer Deadline Must Change (Step 15)**
- 15a. Rescheduling cannot meet original deadline
- 15b. System flags for operations manager review
- 15c. Operations manager contacts customer
- 15d. Customer approves new deadline
- 15e. System commits schedule with new deadline

**A2: Machine Breakdown (Step 3)**
- 3a. Event is machine breakdown
- 3b. System retrieves all jobs in machine's queue
- 3c. System removes machine from available pool
- 3d. System reassigns all jobs to other machines
- 3e. Continue to step 7

**Business Rules:**
- BR-RESCHED-001: Event-driven trigger
- BR-RESCHED-002: Affected jobs only
- BR-RESCHED-003: Priority reprint handling
- BR-SCHED-005: Bounded scheduling computation
- BR-SCHED-006: Feasible fallback guarantee
- BR-SCHED-007: In-progress jobs untouched
- BR-SCHED-008: Customer deadline stability

---

## UC-013: System Creates Reprint Job

**Description:** System automatically creates reprint job when part fails inspection.

**Actors:**
- Primary: System

**Preconditions:**
- Part failed inspection (UC-006)
- Fault attributed to lab or hub

**Postconditions:**
- Reprint job created with URGENT priority
- Job assigned to lab
- Customer notified appropriately

**Main Flow:**
1. System receives inspection failure event
2. System checks fault attribution
3. Fault is LAB or HUB (not customer)
4. System retrieves original job details
5. System creates new job record:
   - Copy model, configuration from original
   - Set priority = URGENT
   - Set deadline = original_order.delivery_date
   - Mark as reprint (links to original job)
6. If fault = LAB: mark cost as "lab_expense"
7. If fault = HUB: mark cost as "platform_expense"
8. System checks reprint count for order
9. Reprint count <= 2 (not exceeded limit)
10. System triggers job assignment (UC-011)
11. System updates original job status to "REPRINTING"
12. System sends notification to customer:
    - "Quality issue detected during inspection"
    - "Reprint initiated at no charge"
    - "Delivery date remains [date]"
13. System logs reprint event

**Alternate Flows:**

**A1: Customer Fault (Step 3)**
- 3a. Fault attributed to CUSTOMER
- 3b. Do not create reprint job automatically
- 3c. Notify customer of geometry issue
- 3d. Offer paid reprint or refund
- 3e. Await customer decision

**A2: Reprint Limit Exceeded (Step 9)**
- 9a. Reprint count > 2
- 9b. Escalate to operations manager
- 9c. Manual review required
- 9d. Operations manager decides action

**Business Rules:**
- BR-RESCHED-003: Priority reprint handling
- BR-RESCHED-004: Reprint limit
- BR-QC-005: Lab fault triggers reprint at lab expense
- BR-QC-006: Customer fault notification
- BR-QC-007: Hub fault platform cost

---

## Use Case Diagram

```
                             PrintGrid System
                                   
    Customer                         Lab                      Hub Staff
       |                              |                           |
       |--UC-001: Place Order         |                           |
       |--UC-003: Track Order         |--UC-004: Accept Job       |
       |--UC-007: Request Reprint     |--UC-005: Execute Print    |
                                      |--UC-008: View Performance |--UC-006: Inspect Quality
                                                                   |--UC-020: Fulfill Order
                                                                   
    Operations Manager           Platform Admin              System (Internal)
       |                              |                           |
       |--UC-033: Monitor Network     |--UC-009: Configure Params |--UC-002: Analyze Model
       |--UC-034: Manual Override     |--UC-029: Manage Users     |--UC-010: Speculative Schedule
       |--UC-035: Generate Reports    |--UC-031: Manage Catalogs  |--UC-011: Assign Job
                                                                   |--UC-012: Reschedule
                                                                   |--UC-013: Create Reprint
```

---

## Summary

**Total Use Cases: 13 Detailed**

| Category | Use Cases |
|----------|-----------|
| Customer | UC-001, UC-003, UC-007 |
| Lab | UC-004, UC-005, UC-008 |
| Hub | UC-006 |
| Admin | UC-009 |
| System/Scheduling | UC-002, UC-010, UC-011, UC-012, UC-013 |

**Key Integration Points:**
- UC-001 → UC-002 → UC-010 → UC-011 (Order to Assignment flow)
- UC-005 → UC-006 → UC-013 (Production to Quality to Reprint flow)
- UC-011 ↔ UC-012 (Assignment and Rescheduling loop)

**Traceability:**
- Each use case references specific Business Rules
- Maps to User Stories
- Provides basis for Acceptance Criteria and Test Cases
