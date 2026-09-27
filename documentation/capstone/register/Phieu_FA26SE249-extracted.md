
 
 
CAPSTONE PROJECT REGISTER 
 
Class:            Duration time:  from 09/2026 To 03/2027
(*) Profession: Software Engineer                   Specialty: SE                           
		(*) Kinds of person make registers:               	Lecturer                	Students  
 
1. Register information for supervisor (if have) 
No.
Full name
Phone
E-Mail 
Title 
  Supervisor 
	Nguyễn Tấn Phúc

phucnt40@fpt.edu.vn
Giảng viên 
2. Register information for students (if have) 
No.
Full name 
Student code 
Phone 
E-mail 
Role in Group 
1




 
2




 
3




 
4




 
5





 
3. Register content of Capstone Project 
(*) 3.1. Capstone Project name: 
3.1.1. English: PrintGrid: Development of a Distributed 3D Printing Fulfillment and Scheduling Platform for a Network of Independent Printing Labs
3.1.2. Vietnamese: PrintGrid: Xây dựng nền tảng điều phối và tối ưu đơn hàng cho mạng lưới 3D Printing Lab phân tán
Abbreviation: PrintGrid
 (*) 3.2. Main proposal content (including result and product)   
a) Context:
Demand for 3D printing services is growing and diversifying, while the printing capacity that could serve it sits scattered across many small labs — university fablabs, maker spaces, small print shops — each with a different mix of machines, technologies, materials and technical skill. When every lab takes its own orders, the customer faces a different price, a different quality standard and a different lead time at each door, and the network as a whole runs its machines badly: one lab turns work away while another sits idle, because neither can see the other's queue. The obvious fix is a single platform that takes the order and distributes the work, but the difficulty is not the storefront. It is that the platform must commit to a price and a delivery date at the moment the customer clicks order, before it knows which machine will produce the part, and must then keep that promise across a production network it does not own and cannot directly control.
	-	Capacity in this domain is not a single number: A lab with three idle printers may still be unable to take a job, because the part exceeds the build volume, the machine cannot hold the required tolerance, or the material and colour are not in stock. Availability is a conjunction of hard constraints, so any allocation that reasons about load alone will assign work that cannot be produced.
	-	A delivery date must be promised before the work is scheduled: Quoting from a fixed lead-time table is what small print shops do, and it fails as soon as the network is loaded — the date is either padded so heavily it loses the order, or optimistic and missed. A credible date can only come from a schedule, which means the platform must schedule speculatively at quote time.
	-	One price must cover a heterogeneous cost base: The same part costs different amounts to produce at different labs, yet the customer must see one price for one specification, independent of where it is eventually made. Pricing and lab cost therefore have to be deliberately decoupled, and the difference absorbed at the network level.
	-	Print time estimates drift, and the schedule is built on them: Estimated durations come from slicing, but real machines run slower or faster depending on age, calibration and operator practice. A schedule that treats estimates as exact accumulates error across a queue and silently turns a promised date into a missed one.
	-	Production failure is routine, not exceptional: A print can fail at eighty percent of its duration, a machine can go down mid-queue, and a part can pass at the lab but fail inspection at the hub. Any of these invalidates part of the plan while work is already in progress, so replanning is a normal operating mode rather than an error path.
	-	Labs self-report their own capability and capacity: Declared machine specifications, material stock and working hours are the inputs the allocator trusts, and a lab's incentive is to accept as much work as possible. Without a mechanism that compares declared capacity against delivered results, the allocation degrades as the network grows.
	-	The platform carries the quality guarantee it cannot directly enforce: The customer buys from the platform and never learns which lab produced the part, so a defect is the platform's failure regardless of where it originated. Quality standards, inspection and fault attribution must therefore live centrally, not be delegated to each lab's own judgement.

b) Proposed Solutions:
Build PrintGrid, a platform where the customer deals with a single system and the network behind it decides who prints what and when. An uploaded model is analysed and sliced server-side to obtain print time and material consumption, priced through one network-wide formula, and then scheduled speculatively across the network so the delivery date offered to the customer is derived from an actual feasible plan rather than a lead-time table. On confirmation the order is decomposed into printing jobs, each filtered against hard capability constraints, scored across time, load, quality history, cost and logistics, and placed into a specific machine's queue. Production events — acceptance, failure, machine breakdown, inspection results — feed back into an event-driven rescheduler that repairs the plan without disturbing work already on the bed. Finished parts converge on a Central Hub for inspection, fault attribution, reprint and packing, and every completed job updates a performance ledger that shapes the next allocation decision.
	-	Geometry and Slicing Analysis Service: Server-side validation and slicing of uploaded STL/OBJ/3MF models to extract bounding box, volume and mesh integrity, and to estimate print duration, primary material and support material for a given print configuration.
	-	Network-Uniform Pricing Engine: A single parameterised formula over material, machine time, post-processing and hub handling, applied identically regardless of the assigned lab, with the parameter set versioned and frozen onto each quote so a quote remains reproducible after prices change.
	-	Capability-Filtered Assignment: A hard feasibility filter over technology, build volume, achievable layer height and tolerance, material and colour stock, machine state and lab standing, followed by a configurable multi-criteria score over deadline slack, remaining capacity, quality history, internal cost and transfer time to the hub.
	-	Deadline-Backward Scheduling with Batch Consolidation: Jobs are placed on individual machines against internal due dates derived by working backwards from the customer deadline through hub transfer, inspection and shipping, with jobs sharing material and colour consolidated on a machine to remove changeover time, bounded so consolidation cannot push urgent work behind a large batch.
	-	Speculative Quote-Time Scheduling: A trial placement run at quote time that returns the earliest feasible completion across the network, so the promised date and the priority surcharge both follow from real capacity rather than from an assumed constant.
	-	Event-Driven Rescheduling: Rejection, failure, breakdown and reprint trigger repair of the unstarted portion of the plan only, under a bounded computation budget, with the dispatching-rule solution always retained as a feasible fallback.
	-	Estimate Calibration Loop: Actual print durations reported by labs are compared against slicer estimates per machine model, and the resulting correction factors are fed back into both quoting and scheduling so the network's promises improve as it accumulates history.
	-	Central Hub Quality Control and Fault Attribution: Inspection against a checklist bound to the ordered quality grade, defect classification, attribution of each failure to lab, hub or customer-supplied geometry, and automatic creation of prioritised reprint jobs that inherit the original deadline.
	-	Lab Performance Ledger: An auditable record of on-time delivery, first-pass yield, acceptance rate, utilisation and the gap between declared and delivered capacity, aggregated into a standing that is an explicit input to the assignment score.
	-	Model File Access Control: Customer models held in object storage and released to a lab only for the duration of an assigned job through short-lived, revocable links, so a lab never accumulates a library of other people's designs.

c) Functional Requirements:
	-	Customer:
	•	Register, sign in and manage the account and delivery addresses
	•	Upload STL/OBJ/3MF files and inspect them in an in-browser 3D viewer with bounding-box and dimension readout
	•	Receive validation feedback on mesh integrity and on parts exceeding the largest build volume in the network
	•	Specify per-item print configuration: material, colour, layer height, infill, post-processing and quantity
	•	Choose a service level and a required-by date, and see immediately whether that date is achievable
	•	Receive an automatic quote with a cost breakdown, a committed delivery date and an expiry time
	•	Confirm the order and pay
	•	Track order and item status in real time without visibility of which lab is producing the part
	•	Raise a reprint or complaint request after delivery within the configured window
	•	Manage a personal library of uploaded models and reuse them in later orders

	-	Lab Manager:
	•	Register the lab and declare working calendar, capacity and transfer time to the hub
	•	Maintain the machine registry: technology, build volume, achievable layer height and tolerance, and operational state
	•	Maintain material and colour inventory with stock levels and consumption tracking
	•	Accept or decline assigned jobs within a bounded response window, with a reason on decline
	•	View the machine-level schedule proposed by the platform as a timeline
	•	View the lab's own performance standing and the figures behind it

	-	Lab Operator:
	•	View the job queue for a machine, with model file, print configuration and internal due time
	•	Download the model file for an assigned job through a time-limited link
	•	Advance job state through preparation, printing, post-processing and completion
	•	Report actual print duration and material consumed on completion
	•	Report production incidents — print failure, machine breakdown, material shortage — with photographic evidence
	•	Record handover of finished parts to the hub

	-	Hub Quality Control Staff:
	•	Receive incoming batches from labs and reconcile them against expected jobs
	•	Execute the inspection checklist bound to each item's quality grade
	•	Record pass or fail per unit with defect classification and photographic evidence
	•	Attribute each failure to lab, hub or customer-supplied geometry
	•	Trigger reprint jobs and confirm the revised delivery commitment

	-	Hub Fulfillment Staff:
	•	Consolidate items belonging to one order and confirm completeness before packing
	•	Pack, record shipment details and hand over to the carrier
	•	Update delivery status through to completion

	-	Operations Manager:
	•	Monitor network state: open orders, jobs at risk of lateness, load and standing per lab
	•	Override an assignment or a schedule placement, with a recorded reason
	•	Adjust order priority and intervene on orders exceeding the reprint limit
	•	Suspend or reinstate a lab, and cap the work assigned to a lab under observation
	•	View SLA dashboards and export performance reports

	-	Pricing and Scheduling Engine:
	•	Slice and analyse uploaded models to estimate print time and material consumption
	•	Compute price from the active parameter set and freeze that version onto the quote
	•	Run a speculative network-wide placement at quote time to derive the committed delivery date
	•	Decompose confirmed orders into printing jobs by item, quantity and build-volume limits
	•	Filter labs by hard capability constraints and score the feasible set
	•	Place jobs on specific machines against backward-derived internal due dates, with material and colour consolidation
	•	Repair the unstarted portion of the schedule on rejection, failure, breakdown or reprint, within a bounded time budget
	•	Recalibrate duration estimates per machine model from reported actuals
	•	Record every assignment decision with the candidate set and scores for later review

	-	System Administrator:
	•	Manage accounts, roles and permissions across customers, labs and hub staff
	•	Maintain catalogues of materials, colours, technologies, quality grades and post-processing services
	•	Configure pricing parameters, assignment weights, scheduling limits and SLA thresholds without redeployment
	•	Manage inspection checklists and the defect taxonomy
	•	Monitor background job health, storage and integration status
	•	Review the audit log of manual interventions and parameter changes

d) Non-Functional Requirements:
	-	Quote Responsiveness: Slicing and speculative placement are expensive, so quoting must run asynchronously with a fast preliminary estimate returned immediately and the committed figure delivered on completion; the customer must never be left facing a blocked screen while a large mesh is processed.
	-	Bounded Scheduling Computation: Rescheduling happens while production is running, so the optimiser must operate under a hard time budget and always hold a feasible dispatching-rule solution, since a late plan is worse than a slightly worse plan delivered on time.
	-	Feasibility Correctness: A job must never be assigned to a machine that cannot physically produce it. Capability filtering is a correctness requirement rather than a quality-of-result concern, and must be verified independently of the scoring logic.
	-	Schedule Consistency Under Concurrency: Assignment, manual override and rescheduling can act on the same machine queue simultaneously, so placement must be serialised per machine and double-booking must be impossible by construction rather than by convention.
	-	Stability of Committed Promises: Once a delivery date is committed to a customer it may only move under defined conditions and with notification; a scheduler that silently reoptimises promised dates destroys the trust the platform exists to create.
	-	Estimation Accuracy and Calibration: The gap between estimated and actual print duration must be measured continuously per machine model and corrected, because every quote, promise and schedule placement rests on that estimate.
	-	Intellectual Property Protection: Customer models must be accessible to a lab only for an assigned job and only for its duration, through short-lived revocable links, with all access logged; models must not be downloadable in bulk by any lab account.
	-	Decision Auditability: Every assignment, reschedule and manual override must be reconstructible after the fact, including the candidate labs considered and their scores, so allocation disputes with labs can be settled from the record.
	-	Configurability Without Redeployment: Pricing parameters, scoring weights, priority coefficients, quality grades and SLA thresholds are commercial settings that will be tuned repeatedly and must be editable through the administration interface.
	-	Shop-Floor Usability: The lab interface is used at a machine, often on a tablet with poor connectivity and by an operator with dirty hands, so status updates must be reachable in a minimum of interactions and tolerate brief loss of connection without losing entered data.
	-	Scalability of the Network Model: The data model and algorithms must accommodate at least fifty labs and several hundred machines without architectural change, since allocation quality only becomes meaningful at network scale.
	-	Fair Work Distribution: Allocation must not concentrate all work on the few highest-scoring labs, as a network whose smaller members receive nothing will lose them and shrink its own capacity; a configurable exploration allowance must keep capable labs in circulation.

e) Theory & Practical:
Theory
The project draws on production scheduling, operations management, computational geometry and mechanism design for multi-party platforms.
	-	Parallel Machine Scheduling with Release and Due Dates: The core allocation problem maps onto scheduling unrelated parallel machines with sequence-dependent setup times and a weighted tardiness objective, a formulation known to be NP-hard, which establishes why the project pursues good feasible plans under a time budget rather than optimal ones.
	-	Dispatching Rules and Local Search Metaheuristics: Earliest Due Date and Apparent Tardiness Cost as constructive rules producing an immediate feasible schedule, and simulated annealing or tabu search over swap and transfer neighbourhoods as improvement procedures, together with the anytime property that makes them usable under interruption.
	-	Batching and Setup Time Reduction: Grouping jobs by material and colour to eliminate changeover, and the tension between batch efficiency and the delay a large batch imposes on urgent work, which is the classical lot-sizing versus responsiveness trade-off.
	-	Order Promising and Capable-to-Promise: The distinction between quoting from a static lead time and deriving a date from available capacity, which is what justifies running a placement trial before the order exists.
	-	Multi-Criteria Decision Analysis: Weighted scoring over normalised criteria, sensitivity of the outcome to weight choice, and the effect of normalisation when criteria have different scales and directions.
	-	Rescheduling and Schedule Stability: Predictive-reactive scheduling, the distinction between full regeneration and repair of an existing plan, and the trade-off between schedule quality and nervousness — the cost of a plan that keeps changing under everyone's feet.
	-	Computational Geometry and Slicing: Mesh validation, watertightness, bounding-volume computation, and the layer-based toolpath generation from which print time and material consumption are derived.
	-	Statistical Calibration of Estimates: Regression of observed durations on slicer estimates per machine class to correct systematic bias, and the use of prediction error as a monitored quality signal.
	-	Reputation and Feedback Mechanisms: Performance scoring as an input to allocation, the cold-start problem for new participants, and the exploration-exploitation tension that arises when past performance determines future opportunity.
	-	Quality Management and Fault Attribution: Acceptance inspection, first-pass yield, defect classification and responsibility assignment in a multi-party production chain.

Practical
These concepts are applied by building the platform and validating it against both a simulated network and a real printing lab.
	-	Implement the slicing and geometry service on an open-source slicing engine, and establish the mapping from print configuration to slicer profile for each supported technology.
	-	Implement the pricing engine with a versioned parameter set and verify that a frozen quote remains reproducible after parameters change.
	-	Implement capability filtering as an independently testable component, with a test suite of jobs deliberately constructed to be infeasible for specific machines.
	-	Implement the scoring function with configurable weights, and examine how allocation outcomes shift as weights change.
	-	Implement backward due-date derivation, machine-level placement, material and colour consolidation, and the batch bound that protects urgent work.
	-	Implement the improvement search under a hard time budget, retaining the dispatching-rule plan as a fallback.
	-	Implement event-driven repair for rejection, print failure, machine breakdown and reprint, verifying that in-progress jobs are never disturbed.
	-	Build a network simulator that generates order streams under configurable load, and a lab simulator that advances jobs on schedule with configurable rates of rejection, print failure and inspection failure.
	-	Compare the proposed allocation against a random baseline and a nearest-available-lab baseline on the same generated workload, reporting weighted tardiness, on-time rate, utilisation, changeover count and load spread across labs.
	-	Run a trial with one real printing lab to compare slicer estimates against actual print durations, calibrate the correction factors, and test the shop-floor interface under real operating conditions.
	-	Test model file access control adversarially against attempts to retrieve files outside an assigned job window.

f) Products (Expected Deliverables):
	-	Customer Web Portal: Model upload with in-browser 3D preview, print configuration, automatic quoting with cost breakdown and committed delivery date, checkout and payment, real-time order tracking, reprint requests and a personal model library.
	-	Lab Portal: Lab and machine registry, material inventory, job acceptance, machine-level schedule timeline, job execution and progress updates, actual duration and incident reporting, handover recording, and the lab's own performance view.
	-	Central Hub Console: Batch receipt and reconciliation, checklist-driven inspection, defect classification and fault attribution, reprint initiation, order consolidation, packing and shipment tracking.
	-	Operations Console: Network monitoring, at-risk job alerts, manual assignment and priority override with recorded justification, lab suspension and capping, SLA dashboards and report export.
	-	Slicing and Geometry Analysis Service: Mesh validation and analysis, print duration and material estimation, and profile mapping per technology and quality grade.
	-	Pricing Engine: Parameterised network-wide price computation, versioned parameter sets, quote generation with expiry and reproducibility.
	-	Assignment and Scheduling Engine: Capability filter, multi-criteria scorer, backward due-date derivation, machine-level placement with consolidation, time-budgeted improvement search, event-driven repair, and a decision log recording candidates and scores.
	-	Estimate Calibration Module: Per-machine-model comparison of estimated against actual durations and the correction factors applied back into quoting and scheduling.
	-	SLA and Performance Analytics: Metric computation per lab, machine, material and period; the lab performance ledger; and the standing that feeds the assignment score.
	-	Simulation Harness: Configurable order generator and lab simulator with injectable rejection, print failure and inspection failure, used for algorithm evaluation and for demonstrating the system at network scale.
	-	Algorithm Evaluation Report: Comparison of the proposed allocation and scheduling approach against baselines on the generated workloads, plus estimate calibration results from the real lab trial.
	-	System Documentation: Requirement specification, architecture and database design, algorithm specification, test plan and results, deployment guide, and user guides for customer, lab, hub and operations roles.

g) Proposed Tasks:
Team of 5 members, each owning one Work Package as the primary role, with cross-package collaboration during integration, simulation runs and the trial with the partner lab. Because allocation quality can only be assessed at network scale, the simulation harness is built early rather than treated as a testing afterthought.
	-	WP1 – Project Management, Domain Analysis and Evaluation (Role: Project Manager / Business Analyst): Scope and requirements with the partner printing lab, analysis of 3D printing service workflows, quality grades and defect taxonomies, pricing model design with the cost structure of real labs, system architecture and data model, design of the evaluation protocol and baselines, and analysis and reporting of simulation and trial results.
	-	WP2 – Assignment and Scheduling Engine (Role: Algorithm Engineer): Capability filtering, multi-criteria scoring, backward due-date derivation, machine-level placement with material and colour consolidation, dispatching-rule construction and time-budgeted improvement search, event-driven repair, speculative quote-time placement, and the assignment decision log.
	-	WP3 – Core Backend, Pricing and Geometry Services (Role: Backend Developer): Order, quote, job, lab, machine and inventory domains, the state machines for orders and jobs, the slicing and geometry analysis service, the versioned pricing engine, model file storage with time-limited revocable access, asynchronous job orchestration and the audit trail.
	-	WP4 – Customer Portal and Operations Console (Role: Frontend Developer): Customer experience including the in-browser 3D viewer, configuration and quoting flow, checkout and order tracking; and the operations console with network monitoring, schedule timelines, override workflows and SLA dashboards.
	-	WP5 – Lab and Hub Applications, Simulation and QA (Role: Full-stack Developer / QA Engineer): Lab portal optimised for shop-floor use with tolerance for intermittent connectivity, hub inspection and fulfillment console, the order and lab simulation harness with fault injection, and integration testing, trial execution and documentation together with the whole team.
4. Other comments (propose all relative things if have): 
	-	The scheduler is the contribution; the storefront is not: Upload, quote, pay and track is an e-commerce flow the team could build competently in a few weeks, and a capstone that stops there is an online shop with a 3D file attached. What makes the topic worth doing is that the platform must promise a date before it knows which machine will do the work, keep that promise across labs it does not control, and repair the plan when a print fails mid-queue. The examining panel should be able to see that difficulty in the demonstration, which means the scheduling and rescheduling behaviour must be visible in the product, not buried in a service.
	-	A delivery date quoted from a lead-time table invalidates the whole premise: If the committed date comes from a constant rather than from a trial placement against real capacity, then the assignment engine is decorative and the platform is no better than the labs it replaces. Speculative quote-time scheduling should be built in the first iteration, even in a crude form, because retrofitting it later means rebuilding the quoting flow.
	-	Failure and reprint belong in the first design, not in a later sprint: Print failure, machine breakdown and inspection rejection are routine in this domain, and a schedule model that assumes jobs always complete cannot absorb them afterwards. The state machines, the reprint path and the repair logic should be designed together, and the lab simulator should be able to inject these faults from the day it exists.
	-	Without a simulator there is nothing to evaluate: No student team will have fifty labs and three hundred printers, and allocation quality is meaningless on three machines and ten orders. The simulation harness is what turns the algorithm from an assertion into a measured result, and it is also what makes a convincing demonstration possible. Treating it as WP5 tooling rather than as a deliverable is the most likely way this project ends with an unproven scheduler.
	-	One real lab is worth more than a large simulation for the estimation problem: Slicer estimates and real print durations diverge in ways no simulator will reproduce, and the calibration loop is only credible if it is fitted against actual machine data. A partnership with a single lab or university fablab, secured early, is sufficient and should be arranged before implementation rather than during it.
	-	Minimum viable scope and extended scope should be agreed at the outset: The viable core is order and quoting with slicing-based estimation, uniform pricing, capability filtering with scored assignment, machine-level scheduling with a dispatching rule, the lab and hub applications, and hub inspection with reprint. Improvement search, estimate calibration, the performance ledger feeding back into allocation, and fair-distribution exploration are the extended scope that distinguishes a strong result — and they are the parts to protect if the team starts overspending on interface polish.
            
Supervisor (If have) 
 (Sign and full name)
 
HCM, date 25/08/2026 
On behalf of Registers  
(Sign and full name) 

 

