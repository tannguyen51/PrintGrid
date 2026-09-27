# Proposed Solutions - PrintGrid Platform

## 1. Solution Overview

PrintGrid is a **distributed 3D printing fulfillment and scheduling platform** where customers interact with a single unified system while a network of independent printing labs fulfills the orders. The platform acts as an intelligent intermediary that:

- **Accepts orders** through a single customer-facing interface
- **Analyzes and prices** work using geometry analysis and network-wide pricing
- **Schedules speculatively** at quote time to promise realistic delivery dates
- **Assigns work** intelligently based on capabilities and multi-criteria scoring
- **Monitors production** and replans dynamically when failures occur
- **Inspects quality** centrally and attributes faults objectively
- **Learns continuously** from actual outcomes to improve future decisions

---

## 2. Core Solution Components

### 2.1 Geometry and Slicing Analysis Service

**Purpose:** Extract actionable information from customer-uploaded 3D models.

**Functionality:**
- **Mesh Validation**: Check for watertightness, manifoldness, and structural integrity
- **Bounding Box Calculation**: Determine minimum build volume required
- **Orientation Analysis**: Identify optimal print orientation
- **Slicing Execution**: Generate toolpaths using open-source slicing engine (e.g., PrusaSlicer, CuraEngine)
- **Print Time Estimation**: Calculate expected print duration from toolpath
- **Material Estimation**: Compute primary material and support material consumption
- **Technology Mapping**: Map customer requirements to appropriate 3D printing technology

**Technical Approach:**
```
Input: STL/OBJ/3MF file + print configuration
↓
1. Parse and validate mesh geometry
2. Compute bounding box and volume
3. Map configuration → slicer profile
4. Execute slicing (async, background job)
5. Parse G-code to extract estimates
6. Store analysis results with model
↓
Output: Print time, material volume, feasibility flags
```

**Technology Stack:**
- **Slicing Engine**: CuraEngine or PrusaSlicer (command-line)
- **Geometry Library**: CGAL or trimesh (Python) for mesh analysis
- **Storage**: MinIO for STL/G-code files
- **Processing**: Hangfire background jobs for async slicing

---

### 2.2 Network-Uniform Pricing Engine

**Purpose:** Provide consistent pricing across the network regardless of which lab fulfills the order.

**Functionality:**
- **Parameterized Formula**: `Price = f(material_cost, machine_time, post_processing, hub_handling)`
- **Versioned Parameters**: Each quote frozen to specific parameter version
- **Cost Components**:
  - Material cost (per gram, varies by material type)
  - Machine time cost (per hour, varies by technology)
  - Post-processing cost (per service type)
  - Hub handling and inspection cost (per quality grade)
  - Shipping cost (per destination zone)
- **Priority Surcharges**: Dynamic pricing based on deadline urgency derived from actual schedule slack

**Pricing Formula Example:**
```
Base_Cost = (Material_Volume_g × Material_Rate_per_g) + 
            (Print_Time_h × Machine_Rate_per_h) +
            Post_Processing_Cost +
            Hub_Handling_Cost

Priority_Multiplier = 1.0 + (Urgency_Factor × (1 - Schedule_Slack / Normal_Lead_Time))

Final_Price = Base_Cost × Priority_Multiplier × Platform_Margin
```

**Key Features:**
- **Version Locking**: Quote stores parameter version ID → reproducible months later
- **Configuration UI**: Admin can adjust parameters without redeployment
- **Audit Trail**: All parameter changes logged with timestamp and user

**Technology Stack:**
- **Configuration**: PostgreSQL JSONB column for versioned parameter sets
- **Service**: .NET pricing service with strategy pattern
- **Admin UI**: React configuration panel

---

### 2.3 Capability-Filtered Assignment Engine

**Purpose:** Ensure jobs are only assigned to labs/machines that can physically produce them.

**Hard Constraint Filters:**

1. **Technology Filter**
   ```csharp
   bool TechnologyMatches(Job job, Machine machine) =>
       machine.Technology == job.RequiredTechnology;
   ```

2. **Build Volume Filter**
   ```csharp
   bool FitsInBuildVolume(Job job, Machine machine) =>
       job.BoundingBox.X <= machine.BuildVolume.X &&
       job.BoundingBox.Y <= machine.BuildVolume.Y &&
       job.BoundingBox.Z <= machine.BuildVolume.Z;
   ```

3. **Tolerance Filter**
   ```csharp
   bool MeetsToleranceRequirement(Job job, Machine machine) =>
       machine.AchievableTolerance <= job.RequiredTolerance;
   ```

4. **Material Availability Filter**
   ```csharp
   bool HasMaterialInStock(Job job, Lab lab) =>
       lab.Inventory.Any(i => 
           i.Material == job.Material &&
           i.Color == job.Color &&
           i.QuantityInStock >= job.MaterialRequired);
   ```

5. **Machine Operational State Filter**
   ```csharp
   bool MachineOperational(Machine machine) =>
       machine.State == MachineState.Operational &&
       !machine.IsUnderMaintenance;
   ```

6. **Lab Standing Filter**
   ```csharp
   bool LabInGoodStanding(Lab lab) =>
       !lab.IsSuspended &&
       lab.PerformanceScore >= MinimumAcceptableScore;
   ```

**Output:** List of `(Lab, Machine)` pairs that satisfy ALL constraints.

**Technology Stack:**
- **Query**: LINQ expressions over PostgreSQL with appropriate indexes
- **Caching**: Redis cache for lab capabilities (TTL: 5 minutes)
- **Service**: Assignment domain service with specification pattern

---

### 2.4 Multi-Criteria Scoring System

**Purpose:** Rank feasible assignments to choose the best match for each job.

**Scoring Criteria:**

| Criterion | Weight | Direction | Normalization |
|-----------|--------|-----------|---------------|
| **Deadline Slack** | 0.30 | More is better | `slack_hours / max_slack_in_batch` |
| **Current Load** | 0.25 | Less is better | `1 - (queue_hours / capacity_hours)` |
| **Quality History** | 0.20 | More is better | `first_pass_yield` (0.0 to 1.0) |
| **Transfer Time** | 0.15 | Less is better | `1 - (transfer_hours / max_transfer)` |
| **Internal Cost** | 0.10 | Less is better | `1 - (lab_cost / max_cost)` |

**Scoring Algorithm:**
```csharp
public double CalculateScore(Job job, Lab lab, Machine machine)
{
    var deadlineSlack = CalculateDeadlineSlack(job, machine);
    var currentLoad = GetCurrentLoad(machine);
    var qualityHistory = lab.PerformanceMetrics.FirstPassYield;
    var transferTime = GetTransferTime(lab);
    var internalCost = EstimateLabCost(job, lab);
    
    var normalizedScores = new Dictionary<string, double>
    {
        ["deadline_slack"] = Normalize(deadlineSlack, 0, maxSlack),
        ["load"] = 1 - Normalize(currentLoad, 0, 1),
        ["quality"] = qualityHistory,
        ["transfer"] = 1 - Normalize(transferTime, 0, maxTransfer),
        ["cost"] = 1 - Normalize(internalCost, minCost, maxCost)
    };
    
    var weights = GetActiveWeights(); // from configuration
    
    return normalizedScores.Sum(kvp => kvp.Value * weights[kvp.Key]);
}
```

**Key Features:**
- **Configurable Weights**: Admin can adjust without redeployment
- **Exploration Allowance**: Small percentage (5-10%) allocated randomly to maintain network diversity
- **Score Logging**: Every assignment records candidate scores for later analysis

**Technology Stack:**
- **Implementation**: C# assignment scoring service
- **Configuration**: PostgreSQL for weight configurations
- **Logging**: Structured logging to PostgreSQL audit table

---

### 2.5 Deadline-Backward Scheduling with Batch Consolidation

**Purpose:** Place jobs on specific machines with realistic completion times that meet customer deadlines.

**Backward Scheduling Logic:**
```
Customer Delivery Date (confirmed)
    ↓ subtract shipping_time
Hub Ship Date
    ↓ subtract packing_time
Hub Inspection Complete Date
    ↓ subtract inspection_time + transfer_time
Lab Handover Date (this is the INTERNAL DUE DATE)
    ↓ subtract print_time + post_processing_time
Machine Start Date (when job must start printing)
```

**Batch Consolidation Rules:**

1. **Group by Material and Color**: Jobs using same material/color can be batched on same machine
2. **Batch Size Limit**: Maximum batch size to prevent blocking urgent jobs
   ```csharp
   const int MAX_BATCH_SIZE = 5; // jobs
   const double MAX_BATCH_DURATION_HOURS = 24; // hours
   ```

3. **Urgency Protection**: Jobs with tight deadlines bypass batches
   ```csharp
   if (job.InternalDueDate - now < URGENT_THRESHOLD)
       bypassBatch = true;
   ```

**Placement Algorithm:**
```
For each job in priority order (by internal due date):
    1. Get feasible (lab, machine) pairs from capability filter
    2. Score each pair using multi-criteria scoring
    3. Select top-scored pair
    4. Check if machine has existing batch with same material/color
    5. If yes and batch not full and job not urgent:
           Add to batch
       Else:
           Create new batch or place individually
    6. Update machine queue and available capacity
```

**Technology Stack:**
- **Algorithm**: C# scheduling service with priority queue
- **Data Structure**: In-memory schedule representation (per-machine job lists)
- **Persistence**: PostgreSQL for committed schedule

---

### 2.6 Speculative Quote-Time Scheduling

**Purpose:** Provide realistic delivery dates at quote time by running a trial placement before the order exists.

**Workflow:**
```
Customer uploads model and selects configuration
    ↓
1. Slicing analysis (async, show progress to customer)
    ↓
2. Once slicing complete:
    a. Calculate base price
    b. Run TRIAL PLACEMENT across network
       - Treat as temporary order
       - Run capability filter → scoring → placement
       - Record earliest feasible completion date
    c. Derive customer delivery date from trial placement
    d. Calculate priority surcharge based on schedule slack
    ↓
3. Present quote with:
    - Total price (including priority surcharge if applicable)
    - Committed delivery date (from trial placement)
    - Cost breakdown
    - Quote expiry time (e.g., 48 hours)
    ↓
4. If customer confirms:
    - Convert trial placement to real order
    - Re-run placement (may differ if network state changed)
    - Commit to schedule
```

**Key Challenges and Solutions:**

**Challenge:** Speculative placement is expensive
- **Solution:** Run async, show preliminary estimate immediately, refine in background

**Challenge:** Network state changes between quote and confirmation
- **Solution:** Quote expiry time limits staleness; re-run placement on confirmation

**Challenge:** Many speculative quotes, few confirmations (wasted computation)
- **Solution:** Acceptable cost for accurate promises; optimize trial placement with faster heuristic

**Technology Stack:**
- **Background Jobs**: Hangfire for async trial placement
- **Cache**: Redis for trial placement results (TTL: quote expiry time)
- **Service**: Scheduling service with "speculative mode" flag

---

### 2.7 Event-Driven Rescheduling

**Purpose:** Repair the schedule when production events invalidate the current plan.

**Events Triggering Rescheduling:**

1. **Lab Rejects Assigned Job**
   - Reason: Discovered infeasibility after acceptance
   - Action: Remove from lab's queue, re-run assignment for this job

2. **Print Failure**
   - Reason: Print failed at X% completion
   - Action: Create reprint job with high priority, re-run placement

3. **Machine Breakdown**
   - Reason: Machine goes offline unexpectedly
   - Action: Reassign all jobs in that machine's queue

4. **Inspection Failure (Lab Fault)**
   - Reason: Part failed quality inspection
   - Action: Create reprint job, update lab's quality score

5. **New Urgent Order**
   - Reason: Rush order needs insertion into schedule
   - Action: Re-run placement with updated priorities

**Rescheduling Constraints:**

✅ **CAN modify:**
- Jobs not yet started
- Job assignments not yet accepted by lab
- Internal due dates (within bounds)

❌ **CANNOT modify:**
- Jobs currently printing
- Jobs already completed
- Customer-facing delivery dates (unless customer notified and agrees)

**Rescheduling Algorithm:**
```csharp
public async Task<RescheduleResult> HandleEvent(ProductionEvent evt)
{
    var startTime = DateTime.UtcNow;
    var timeBudget = TimeSpan.FromSeconds(30); // max 30 seconds
    
    // 1. Identify affected jobs
    var affectedJobs = GetAffectedJobs(evt);
    
    // 2. Remove from current schedule
    foreach (var job in affectedJobs)
        RemoveFromSchedule(job);
    
    // 3. Get current feasible plan (dispatching rule)
    var fallbackPlan = GenerateDispatchingRulePlan(affectedJobs);
    var bestPlan = fallbackPlan;
    
    // 4. Try to improve with time budget
    while (DateTime.UtcNow - startTime < timeBudget)
    {
        var candidate = ImproveSchedule(bestPlan);
        if (candidate.ObjectiveValue < bestPlan.ObjectiveValue)
            bestPlan = candidate;
    }
    
    // 5. Commit best plan found within time budget
    CommitPlan(bestPlan);
    
    // 6. Notify affected labs and customers if needed
    await NotifyStakeholders(bestPlan, affectedJobs);
    
    return new RescheduleResult { Success = true, Plan = bestPlan };
}
```

**Technology Stack:**
- **Event Bus**: MediatR for domain events
- **Background Processing**: Hangfire for rescheduling jobs
- **Notification**: SignalR for real-time updates to UI
- **Service**: Rescheduling service with anytime algorithm pattern

---

### 2.8 Estimate Calibration Loop

**Purpose:** Continuously improve print time accuracy by learning from actual outcomes.

**Calibration Process:**

1. **Data Collection**
   - Operator reports actual print time when job completes
   - Store: `(machine_model, material, estimated_time, actual_time)`

2. **Per-Machine-Model Regression**
   ```python
   # For each machine model (e.g., "Prusa i3 MK3S")
   actual = α + β × estimated + ε
   
   # Fit linear regression
   β_estimated = regression_coefficient
   
   # Correction factor
   correction_factor = β_estimated
   ```

3. **Apply Corrections Forward**
   - When estimating print time for new job on machine model X:
   ```csharp
   var rawEstimate = GetSlicerEstimate(job);
   var correctionFactor = GetCorrectionFactor(machine.Model);
   var calibratedEstimate = rawEstimate * correctionFactor;
   ```

4. **Monitor Prediction Error**
   - Track MAPE (Mean Absolute Percentage Error)
   ```
   MAPE = (1/n) × Σ |actual - estimated| / actual
   ```
   - Alert if MAPE exceeds threshold (e.g., 20%)

**Key Features:**
- **Per-Machine-Model**: Different correction factors for different printer models
- **Minimum Sample Size**: Require N samples before applying correction (e.g., N=10)
- **Outlier Filtering**: Remove extreme outliers (e.g., prints >3 standard deviations from mean)
- **Continuous Update**: Recalculate correction factors weekly

**Technology Stack:**
- **Storage**: PostgreSQL time-series table for actual vs. estimated
- **Computation**: Python/scikit-learn for regression (background job)
- **Service**: .NET calibration service consuming Python results
- **Monitoring**: Grafana dashboard for MAPE trends

---

### 2.9 Central Hub Quality Control and Fault Attribution

**Purpose:** Ensure consistent quality standards and objectively determine fault responsibility.

**Quality Inspection Workflow:**

```
Lab ships completed parts to Central Hub
    ↓
1. Hub receives batch, scans job IDs
2. Reconcile received vs. expected jobs
    ↓
3. For each item:
    a. Retrieve inspection checklist (based on quality grade)
    b. Perform inspection steps
    c. Photograph item
    d. Record pass/fail per check
    ↓
4. If all checks pass:
    - Mark as approved
    - Move to fulfillment queue
    ↓
5. If any check fails:
    a. Classify defect type:
       - Dimensional inaccuracy
       - Surface defect
       - Mechanical failure
       - Wrong material/color
       - Incomplete post-processing
    b. Attribute fault:
       - Lab (production issue) → Lab pays for reprint
       - Hub (handling damage) → Hub pays for reprint
       - Customer (bad geometry) → Customer notified, no auto-reprint
    c. If lab or hub fault:
       - Create reprint job with priority = URGENT
       - Inherit original deadline
       - Re-run assignment (may go to different lab)
    d. Update lab's quality metrics
    ↓
6. Generate fault attribution report (with photos)
7. Store in audit trail
```

**Inspection Checklist Structure:**
```json
{
  "quality_grade": "standard",
  "checklist": [
    {
      "check_id": "dimensional",
      "description": "Verify critical dimensions within ±0.5mm",
      "mandatory": true
    },
    {
      "check_id": "surface_finish",
      "description": "No visible layer separation or warping",
      "mandatory": true
    },
    {
      "check_id": "color_match",
      "description": "Color matches order specification",
      "mandatory": true
    },
    {
      "check_id": "support_removal",
      "description": "All support material cleanly removed",
      "mandatory": false
    }
  ]
}
```

**Fault Attribution Rules:**
```csharp
public FaultResponsibility AttributeFault(InspectionResult result)
{
    if (result.DefectType == DefectType.DimensionalInaccuracy ||
        result.DefectType == DefectType.WrongMaterial ||
        result.DefectType == DefectType.IncompletePostProcessing)
        return FaultResponsibility.Lab;
    
    if (result.DefectType == DefectType.PhysicalDamage &&
        result.PhotoEvidence.ShowsTransitDamage)
        return FaultResponsibility.Hub;
    
    if (result.DefectType == DefectType.GeometryIssue)
        return FaultResponsibility.Customer;
    
    // Ambiguous cases require manual review
    return FaultResponsibility.RequiresReview;
}
```

**Technology Stack:**
- **Checklist Storage**: PostgreSQL JSONB for flexible checklist definitions
- **Photo Storage**: MinIO for inspection photos
- **Service**: Quality control domain service
- **UI**: React inspection console with photo upload

---

### 2.10 Lab Performance Ledger

**Purpose:** Track objective performance metrics to inform future assignment decisions.

**Metrics Tracked:**

1. **On-Time Delivery Rate**
   ```
   ODR = (Jobs completed by internal due date) / (Total jobs completed)
   ```

2. **First-Pass Yield**
   ```
   FPY = (Jobs passing inspection first time) / (Total jobs submitted to hub)
   ```

3. **Acceptance Rate**
   ```
   AR = (Jobs accepted) / (Jobs assigned)
   ```

4. **Utilization**
   ```
   Utilization = (Actual print hours) / (Available machine hours)
   ```

5. **Capacity Accuracy**
   ```
   CA = (Declared capacity) - (Actual throughput)
   ```

6. **Average Response Time**
   ```
   ART = Average time to accept/reject assigned job
   ```

**Performance Score Calculation:**
```csharp
public double CalculatePerformanceScore(Lab lab, TimeSpan period)
{
    var metrics = GetMetrics(lab, period);
    
    var score = 
        0.35 × metrics.OnTimeDeliveryRate +
        0.30 × metrics.FirstPassYield +
        0.15 × metrics.AcceptanceRate +
        0.10 × NormalizeUtilization(metrics.Utilization) +
        0.10 × (1 - NormalizeCapacityGap(metrics.CapacityAccuracy));
    
    return Math.Max(0, Math.Min(1, score)); // clamp to [0, 1]
}
```

**Standing Tiers:**
- **Excellent**: Score ≥ 0.85 → Priority consideration, premium jobs
- **Good**: Score ≥ 0.70 → Normal allocation
- **Acceptable**: Score ≥ 0.50 → Reduced allocation, probationary
- **Poor**: Score < 0.50 → Suspended pending review

**Key Features:**
- **Sliding Window**: Metrics calculated over last 90 days to avoid stale data
- **Minimum Sample Size**: Require minimum 10 jobs before scoring
- **Transparent Display**: Labs can see their own scores and metrics
- **Appeal Process**: Labs can request review of disputed metrics

**Technology Stack:**
- **Storage**: PostgreSQL with materialized views for performance queries
- **Computation**: Scheduled Hangfire job (daily recalculation)
- **Service**: Analytics service
- **UI**: React dashboard for lab performance

---

### 2.11 Model File Access Control

**Purpose:** Protect customer intellectual property by limiting lab access to model files.

**Access Control Mechanism:**

1. **Storage**
   - Customer STL/3MF files stored in MinIO object storage
   - Files encrypted at rest
   - Access requires signed URL

2. **Access Grant**
   ```csharp
   public async Task<string> GrantModelAccess(Job job, Lab lab)
   {
       if (job.AssignedLabId != lab.Id)
           throw new UnauthorizedException();
       
       var expiryTime = job.ExpectedStartTime + job.EstimatedDuration + TimeSpan.FromHours(4);
       
       var presignedUrl = await _minioClient.PresignedGetObjectAsync(
           bucket: "customer-models",
           objectName: job.ModelFileKey,
           expiryInSeconds: (int)(expiryTime - DateTime.UtcNow).TotalSeconds
       );
       
       await _auditLog.LogModelAccess(job.Id, lab.Id, expiryTime);
       
       return presignedUrl;
   }
   ```

3. **Access Revocation**
   - Automatically expires after time window
   - Manually revoked if job reassigned
   - Cannot generate new URL once job complete

4. **Access Logging**
   - Every URL generation logged with job, lab, timestamp
   - Every file download logged (S3 access logs)
   - Audit trail for IP investigation

**Security Features:**
- Short-lived URLs (typically 24-48 hours max)
- Tied to specific job assignment
- Cannot access other customers' files
- Download tracking
- Automatic cleanup after job completion

**Technology Stack:**
- **Storage**: MinIO with bucket policies
- **Access Control**: Pre-signed URLs with expiry
- **Logging**: PostgreSQL audit table + MinIO access logs
- **Service**: File access service

---

## 3. Integration Architecture

### 3.1 System Context Diagram
```
Customer → [PrintGrid Platform] → Lab Network
                   ↓
            Central Hub
                   ↓
            Shipping Carrier
```

### 3.2 Module Communication
```
React Frontend (Customer/Lab/Hub/Admin)
    ↓ HTTP/REST API
.NET Backend (Modular Monolith)
    ├─ Customer Module
    ├─ Order Module
    ├─ Scheduling Module (Core)
    ├─ Lab Module
    ├─ Quality Module
    ├─ Analytics Module
    └─ Admin Module
    ↓
PostgreSQL (primary data)
Redis (cache + job queue)
MinIO (file storage)
Hangfire (background jobs)
```

---

## 4. Key Differentiators

### What Makes PrintGrid Different:

1. ✅ **Real-time Capacity-Based Promises**: Not lead-time tables
2. ✅ **Speculative Quote-Time Scheduling**: Accurate delivery dates before order
3. ✅ **Event-Driven Adaptive Replanning**: Handles routine production failures
4. ✅ **Continuous Calibration**: Learns from actual outcomes
5. ✅ **Central Quality Control**: Objective fault attribution
6. ✅ **Fair Network Management**: Balances efficiency with network health
7. ✅ **IP Protection**: Time-limited, audited model access

---

## Summary

The PrintGrid solution transforms a fragmented network of printing labs into a **coordinated, intelligent production system** that:

- Promises realistic delivery dates derived from actual capacity
- Assigns work intelligently across capability and quality dimensions
- Adapts continuously to production realities
- Maintains quality standards without owning production assets
- Protects customer IP while enabling distributed manufacturing
- Learns and improves from every completed job
