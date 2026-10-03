# Main Features - PrintGrid Platform

## 1. Customer Features

### F1.1 Intelligent 3D Model Upload and Analysis
**Description:**
Customers can upload 3D models in standard formats (STL, OBJ, 3MF) and receive instant validation and analysis.

**Key Capabilities:**
- ✅ In-browser 3D model viewer with rotation, zoom, pan
- ✅ Automatic mesh validation (watertightness, manifoldness)
- ✅ Bounding box display with exact dimensions
- ✅ Build volume feasibility check across network
- ✅ Geometry issue detection and reporting
- ✅ Multi-file upload for batch orders

**User Experience:**
```
Upload File → Preview in 3D viewer → See dimensions → Get validation feedback
    ↓
✅ "Your model is valid and can be printed by 23 labs in our network"
OR
❌ "Issues detected: Non-manifold edges at vertices 1523, 1524. Auto-repair available."
```

**Technical Implementation:**
- Frontend: React + Three.js for 3D rendering
- Backend: Geometry analysis service with CGAL/trimesh
- Storage: MinIO for model files
- Processing: Async validation via Hangfire

---

### F1.2 Smart Print Configuration
**Description:**
Guided configuration interface that helps customers select appropriate print settings.

**Configuration Options:**
1. **Material Selection**
   - PLA, ABS, PETG, Nylon, TPU, Resin, etc.
   - Material property guidance (strength, flexibility, temperature resistance)
   - Price impact preview

2. **Color Selection**
   - Available colors per material
   - Network-wide availability indicator
   - Color matching advice

3. **Quality Grade**
   - Draft (0.3mm layer height) - Fast and economical
   - Standard (0.2mm layer height) - Balanced quality and speed
   - High (0.1mm layer height) - Detailed finish
   - Ultra (0.05mm layer height) - Maximum detail

4. **Infill Density**
   - 10% (lightweight), 30% (standard), 50% (strong), 100% (solid)
   - Strength vs. cost trade-off visualization

5. **Post-Processing**
   - Support removal (included)
   - Sanding and smoothing
   - Painting
   - Vapor smoothing (for ABS)
   - Assembly services

6. **Quantity**
   - Single unit or batch production
   - Volume discount preview

**Smart Recommendations:**
```
"For mechanical parts under stress, we recommend PETG with 50% infill"
"For decorative items, PLA with 30% infill and sanding is cost-effective"
```

---

### F1.3 Real-Time Quote with Capacity-Derived Delivery Date
**Description:**
Industry-leading quoting system that provides accurate delivery dates based on actual network capacity.

**Quote Process:**
```
1. Customer completes configuration
    ↓
2. System shows "Analyzing..." with progress
    ↓ (30-60 seconds for complex models)
3. Background processes:
   - Slice model with selected settings
   - Estimate print time and material
   - Run trial placement across network
   - Calculate price with breakdown
   - Determine earliest feasible delivery
    ↓
4. Present comprehensive quote:
   ┌─────────────────────────────────────┐
   │ Your Quote (Valid for 48 hours)     │
   ├─────────────────────────────────────┤
   │ Total Price: $47.50                 │
   │                                      │
   │ Cost Breakdown:                     │
   │ - Material (25g PETG): $8.00        │
   │ - Print Time (3.5hrs): $24.50       │
   │ - Support Removal: $5.00            │
   │ - Quality Inspection: $5.00         │
   │ - Shipping: $5.00                   │
   │                                      │
   │ Estimated Print Time: 3h 28min      │
   │ Delivery Date: Sep 8, 2026          │
   │                                      │
   │ ✅ 23 labs can fulfill this order   │
   │                                      │
   │ [Confirm Order] [Modify Config]     │
   └─────────────────────────────────────┘
```

**Key Differentiators:**
- ⭐ Delivery date from actual schedule, not lead-time table
- ⭐ Price transparency with itemized breakdown
- ⭐ Quote expiry prevents stale commitments
- ⭐ Instant feedback on feasibility

**Priority Expediting:**
```
"Need it faster? We can deliver by Sep 5 for $58.50 (+$11 rush fee)"
```

---

### F1.4 Seamless Checkout and Payment
**Description:**
Simple, secure payment processing with order confirmation.

**Payment Features:**
- Multiple payment methods (Credit Card, PayPal, etc.)
- Saved payment methods for repeat customers
- Order summary before payment
- Instant email confirmation with order details
- Invoice generation

---

### F1.5 Real-Time Order Tracking
**Description:**
Live visibility into order status without revealing which lab is producing.

**Tracking Stages:**
```
1. ⏳ Order Confirmed - Assigning to production facility
2. 🔧 In Production - Your part is being printed
3. ✅ Printing Complete - Moving to quality control
4. 🔍 Quality Inspection - Checking your part
5. 📦 Packing - Preparing for shipment
6. 🚚 Shipped - En route to you (Tracking: XYZ123)
7. ✅ Delivered - Enjoy your part!
```

**Real-Time Updates:**
- Email notifications at each stage
- In-app notifications
- Estimated completion time (updates dynamically)
- Ability to contact support from tracking page

**Privacy Protection:**
- Customer never sees which lab is producing
- Platform maintains unified brand experience
- Lab details abstracted

---

### F1.6 Personal Model Library
**Description:**
Customers can manage uploaded models and reorder easily.

**Library Features:**
- All previously uploaded models stored
- Thumbnail previews
- Model metadata (dimensions, upload date)
- One-click reorder with saved configurations
- Model sharing (future feature)
- Organization by project/folder

**Reorder Flow:**
```
Library → Select Model → Previous Config Pre-filled → Get New Quote → Order
```

---

### F1.7 Quality Guarantee and Reprint Requests
**Description:**
Customer protection with easy reprint/complaint process.

**Guarantee Terms:**
- Parts match specifications or free reprint
- Dimensional accuracy guarantee
- Material and color match guarantee
- 30-day quality guarantee window

**Reprint Request Flow:**
```
Order Details → "Request Reprint" → 
Upload Photos → Describe Issue → 
Submit → Platform Reviews → 
Approved → Reprint Prioritized → No Charge
```

---

## 2. Lab Features

### F2.1 Lab Registration and Profile Management
**Description:**
Labs can join the platform and maintain their capabilities.

**Registration Information:**
- Lab name and location
- Business hours and working calendar
- Distance/transfer time to hub
- Bank account for payments
- Contact information

**Lab Profile:**
- Description and photos
- Certifications (if any)
- Years in operation

---

### F2.2 Machine Registry and Management
**Description:**
Comprehensive machine catalog with specifications.

**Machine Information:**
```json
{
  "machine_id": "PRU-001",
  "name": "Prusa i3 MK3S #1",
  "technology": "FDM",
  "build_volume": {
    "x_mm": 250,
    "y_mm": 210,
    "z_mm": 210
  },
  "achievable_layer_height": {
    "min_mm": 0.05,
    "max_mm": 0.35
  },
  "achievable_tolerance_mm": 0.2,
  "status": "operational",
  "maintenance_schedule": "weekly"
}
```

**Machine State Management:**
- Mark as operational / under maintenance / offline
- Schedule downtime for planned maintenance
- Report breakdowns immediately
- View machine utilization statistics

---

### F2.3 Material Inventory Management
**Description:**
Track material stock levels and consumption.

**Inventory Features:**
- Material type, color, quantity in stock
- Low stock alerts
- Consumption tracking per job
- Restock history
- Projected depletion date based on current queue

**Inventory Entry:**
```
Material: PETG
Color: Signal Red
Quantity: 2.5 kg
Unit Cost: $25/kg
Reorder Point: 500g
Supplier: Fillamentum
```

---

### F2.4 Job Assignment Acceptance/Rejection
**Description:**
Labs review assigned jobs and accept or decline within time window.

**Assignment Notification:**
```
┌────────────────────────────────────────┐
│ New Job Assigned: JOB-2847            │
├────────────────────────────────────────┤
│ Machine: Prusa i3 MK3S #1             │
│ Material: PETG, Signal Red             │
│ Estimated Time: 3h 28min               │
│ Material Required: 25g                 │
│ Internal Due: Sep 4, 2026 14:00       │
│                                         │
│ [View Model] [Accept] [Reject]         │
│                                         │
│ ⏰ Respond within 2 hours              │
└────────────────────────────────────────┘
```

**Rejection Requires Reason:**
- Material out of stock
- Machine actually unavailable
- Infeasibility discovered
- Capacity overbooked

**Penalty for Rejection:**
- Tracked in acceptance rate metric
- Frequent rejection lowers assignment priority
- But honest rejection better than failed production

---

### F2.5 Machine-Level Schedule Timeline
**Description:**
Visual timeline showing job queue per machine.

**Timeline View:**
```
Prusa i3 MK3S #1
├─ [JOB-2801] ████████░░░░░░  (In Progress: 53% complete)
├─ [JOB-2847] ░░░░░░░░░░  (Queued: 3.5h estimated)
├─ [JOB-2850] ░░░░░░  (Queued: 2.1h estimated)
└─ [JOB-2851] ░░░░░░░░  (Queued: 2.8h estimated)

Total Queue: 8.4 hours
Est. All Complete: Sep 4, 18:30
```

**Interactive Features:**
- Click job to see details
- Color-coded by priority (normal, urgent, reprint)
- Hover to see internal due dates
- Filter by material type

---

### F2.6 Production Workflow for Operators
**Description:**
Streamlined interface for shop-floor staff, optimized for tablets.

**Operator Dashboard:**
```
Current Queue: Prusa i3 MK3S #1
┌─────────────────────────────────────┐
│ 🔴 NOW: JOB-2801                   │
│ Status: Printing (53% complete)     │
│ Est. Completion: 1h 37min           │
│                                      │
│ [Report Issue] [Mark Complete]      │
├─────────────────────────────────────┤
│ ⏭ NEXT: JOB-2847                   │
│ Material: PETG Signal Red           │
│ Est. Time: 3h 28min                 │
│ Due: Sep 4, 14:00                   │
│                                      │
│ [Download Model] [Start Print]      │
└─────────────────────────────────────┘
```

**Workflow Stages:**
1. **Download Model**: Get time-limited access to STL file
2. **Prepare**: Slice, load material, calibrate bed
3. **Start Print**: Begin printing, mark status
4. **Monitor**: Update progress (optional)
5. **Post-Process**: Remove supports, sand if required
6. **Complete**: Report actual time and material used
7. **Handover**: Package and ship to hub

**Mobile-Optimized:**
- Large touch targets for gloved hands
- Works offline (sync when connected)
- Minimal data entry
- Photo upload for issues

---

### F2.7 Incident Reporting
**Description:**
Quick incident reporting with photographic evidence.

**Incident Types:**
- Print failure (with stage: first layer, mid-print, final layer)
- Machine breakdown
- Material shortage
- Quality concern

**Incident Form:**
```
Incident Type: Print Failure
Job ID: JOB-2847
Machine: Prusa i3 MK3S #1
Stage: 78% complete
Description: Layer separation, part warped off bed
Photos: [Upload Photos]
[Submit Incident]
```

**Platform Response:**
- Automatic rescheduling triggered
- Operations manager notified if critical
- Incident added to machine history

---

### F2.8 Performance Dashboard for Labs
**Description:**
Transparent view of lab's own performance metrics.

**Metrics Display:**
```
Lab Performance (Last 90 Days)
┌────────────────────────────────────┐
│ Overall Score: 0.82 (Good)         │
├────────────────────────────────────┤
│ On-Time Delivery: 87% ✅           │
│ First-Pass Yield: 92% ✅           │
│ Acceptance Rate: 95% ✅            │
│ Avg. Utilization: 68%              │
│ Revenue This Month: $2,340         │
│                                     │
│ Top Issues:                         │
│ • 3 late deliveries (machine down) │
│ • 2 inspection failures (support)  │
└────────────────────────────────────┘
```

**Improvement Recommendations:**
- Identifies patterns in defects
- Compares to network average (anonymized)
- Suggests operational improvements

---

## 3. Hub Features

### F3.1 Batch Receipt and Reconciliation
**Description:**
Hub staff track incoming shipments from labs.

**Receipt Workflow:**
```
1. Scan batch barcode from lab
2. System shows expected jobs in batch
3. Scan each item QR code
4. Reconcile: ✅ All items received OR ❌ Missing items
5. Alert if discrepancies
```

**Missing Item Handling:**
- Automatic notification to lab
- Investigation workflow
- Rescheduling triggered if needed

---

### F3.2 Quality Inspection Console
**Description:**
Checklist-driven inspection with defect classification.

**Inspection Flow:**
```
1. Select job from queue
2. Load checklist (based on quality grade)
3. Step through checks:
   ✅ Dimensional accuracy within spec
   ✅ No visible defects (warping, layer separation)
   ✅ Correct material and color
   ✅ Support material fully removed
   ✅ Surface finish meets standard
4. Photograph item (all angles)
5. Overall result: PASS or FAIL
6. If FAIL:
   - Classify defect type
   - Attribute fault (Lab/Hub/Customer)
   - Trigger reprint if needed
7. Mark inspection complete
```

**Defect Classification:**
- Dimensional inaccuracy
- Surface defect
- Mechanical failure
- Wrong material/color
- Incomplete post-processing
- Physical damage (transit)
- Geometry issue (customer file)

**Fault Attribution:**
- Lab fault → Lab performance score reduced, reprint charged to lab
- Hub fault → Hub process review, reprint platform cost
- Customer fault → Customer notified, reprint requires payment

---

### F3.3 Reprint Management
**Description:**
Streamlined workflow for initiating and tracking reprints.

**Reprint Trigger:**
```
Inspection FAIL (Lab Fault) →
Automatic Reprint Job Created:
  - Priority: URGENT
  - Inherit original deadline
  - Assigned via normal allocation (may go to different lab)
  - Original job marked as "Reprinting"
```

**Customer Communication:**
```
"We identified a quality issue with your part during our inspection.
We've immediately initiated a reprint at no charge to you.
Your delivery date remains Sep 8, 2026.
We apologize for the inconvenience."
```

---

### F3.4 Order Consolidation and Fulfillment
**Description:**
Combine all items from an order for shipment.

**Consolidation Workflow:**
```
1. View orders ready for fulfillment
2. For each order, verify all items inspected and passed
3. If complete → move to packing queue
4. If incomplete → show missing items and ETA
```

**Packing:**
- Print packing slip
- Protective packaging based on fragility
- Record package dimensions and weight
- Generate shipping label
- Scan package to carrier
- Update customer tracking

---

## 4. Operations Manager Features

### F4.1 Network Monitoring Dashboard
**Description:**
Real-time visibility into entire network state.

**Dashboard Widgets:**
```
┌───────────────────┬───────────────────┐
│ Active Orders: 47 │ At Risk: 3 🔴    │
├───────────────────┼───────────────────┤
│ Jobs Printing: 12 │ Queued: 35       │
├───────────────────┼───────────────────┤
│ Network Load: 72% │ Avg. Lead: 4.2d  │
└───────────────────┴───────────────────┘

Labs Status:
✅ 18 labs operational
⚠️  2 labs at capacity
🔴 1 lab offline (maintenance)

Recent Alerts:
🔴 JOB-2843: Behind schedule, due in 2h
⚠️  Lab-07: 3rd rejection today
✅  All inspections on schedule
```

**Drill-Down:**
- Click any metric to see details
- Filter by lab, time range, priority
- Export data for analysis

---

### F4.2 Manual Intervention Tools
**Description:**
Override automated decisions when necessary.

**Intervention Actions:**
- **Reassign Job**: Manually move job to different lab/machine
  - Requires justification
  - System validates feasibility
- **Adjust Priority**: Bump order to urgent or deprioritize
  - Triggers rescheduling
- **Extend Deadline**: Renegotiate delivery date
  - Requires customer notification approval
- **Suspend Lab**: Temporarily stop assignments
  - Specify reason and duration
- **Cap Lab**: Limit max jobs assigned to lab
  - Useful for probationary period

**Audit Trail:**
Every intervention logged with:
- User who made change
- Timestamp
- Justification
- Impact (jobs affected, customers notified)

---

### F4.3 SLA Monitoring and Reporting
**Description:**
Track service level agreement compliance.

**SLA Metrics:**
- On-time delivery rate (target: 95%)
- Quote-to-delivery accuracy (target: 90%)
- Quality defect rate (target: <5%)
- Reprint rate (target: <3%)
- Customer satisfaction score

**Reporting:**
- Daily/Weekly/Monthly reports
- Trend analysis
- Lab-level breakdown
- Export to Excel/PDF

---

## 5. Platform Administrator Features

### F5.1 User and Role Management
**Description:**
Centralized access control.

**User Types:**
- Customers
- Lab Managers
- Lab Operators
- Hub QC Staff
- Hub Fulfillment Staff
- Operations Managers
- Platform Admins

**Permissions:**
- Role-based access control (RBAC)
- Fine-grained permissions
- Multi-lab access for operators
- Temporary access grants

---

### F5.2 Configuration Management
**Description:**
Adjust business parameters without redeployment.

**Configurable Parameters:**

1. **Pricing**
   - Material rates (per gram per material type)
   - Machine time rates (per hour per technology)
   - Post-processing service costs
   - Hub handling costs
   - Shipping rates per zone

2. **Assignment Weights**
   - Deadline slack weight: 0.30
   - Current load weight: 0.25
   - Quality history weight: 0.20
   - Transfer time weight: 0.15
   - Internal cost weight: 0.10

3. **Scheduling Limits**
   - Max batch size: 5 jobs
   - Max batch duration: 24 hours
   - Urgent threshold: 48 hours
   - Rescheduling time budget: 30 seconds

4. **SLA Thresholds**
   - On-time target: 95%
   - Quality target: 95%
   - Minimum lab score: 0.50

5. **Quality Grades**
   - Draft, Standard, High, Ultra
   - Inspection checklists per grade

**Configuration UI:**
```
┌─────────────────────────────────────┐
│ Configuration: Pricing Parameters   │
├─────────────────────────────────────┤
│ PLA Rate: $0.32 / gram              │
│ PETG Rate: $0.38 / gram             │
│ ABS Rate: $0.35 / gram              │
│ ...                                  │
│                                      │
│ [Save as New Version]                │
│                                      │
│ ⚠️ Changes create new parameter     │
│    version. Existing quotes use     │
│    their frozen version.            │
└─────────────────────────────────────┘
```

---

### F5.3 Catalog Management
**Description:**
Maintain reference data.

**Catalogs:**
- Materials (name, properties, default rates)
- Colors (name, availability, premium pricing)
- Technologies (FDM, SLA, SLS, MJF, descriptions)
- Post-processing services (name, description, cost)
- Quality grades (name, description, inspection checklist)
- Defect types (classification taxonomy)

---

### F5.4 System Monitoring
**Description:**
Health checks and diagnostics.

**Monitoring:**
- Background job status (Hangfire dashboard)
- API response times
- Database performance
- Storage usage (MinIO)
- Cache hit rates (Redis)
- Error rates and logs

**Alerts:**
- Jobs failing repeatedly
- Performance degradation
- Storage approaching limit
- Security events

---

### F5.5 Audit Log Viewer
**Description:**
Complete audit trail for compliance and investigation.

**Logged Events:**
- User logins and actions
- Configuration changes
- Manual interventions
- Model file access
- Payment transactions
- Order state changes

**Search and Filter:**
- By user, event type, date range
- Export for external analysis
- Tamper-proof logging

---

## 6. Cross-Cutting Features

### F6.1 Real-Time Notifications
**Description:**
Push notifications via multiple channels.

**Notification Types:**
- Order status updates
- Job assignments
- Quality issues
- Payment confirmations
- System alerts

**Channels:**
- Email
- In-app notifications
- SMS (optional, for urgent)
- SignalR real-time updates

---

### F6.2 Multi-Language Support (Future)
**Description:**
Interface localization for global expansion.

**Supported Languages (Planned):**
- English (default)
- Vietnamese
- Chinese
- Spanish

---

### F6.3 Mobile Responsiveness
**Description:**
All interfaces optimized for mobile devices.

**Mobile-Optimized:**
- Customer portal (full mobile experience)
- Lab operator interface (tablet-optimized)
- Hub console (tablet-optimized)
- Operations dashboard (desktop-primary, mobile-readable)

---

### F6.4 API for Integrations (Future)
**Description:**
REST API for third-party integrations.

**API Capabilities:**
- Submit orders programmatically
- Check quote prices
- Track order status
- Retrieve invoices

**Use Cases:**
- CAD software plugins
- E-commerce platform integrations
- ERP system connections

---

## Summary: Feature Prioritization

### ✅ MVP (Minimum Viable Product)
- F1.1-1.3: Upload, config, quote
- F1.4-1.5: Checkout, tracking
- F2.1-2.6: Lab management and production
- F3.1-3.4: Hub QC and fulfillment
- F4.1: Network monitoring
- F5.1-5.2: User management, config

### 🎯 Extended Scope (Strong Result)
- F1.6: Model library
- F1.7: Reprint requests
- F2.7-2.8: Incident reporting, performance dashboard
- F4.2-4.3: Manual interventions, SLA reports
- F5.3-5.5: Catalogs, monitoring, audit logs

### 🚀 Future Enhancements
- F6.2: Multi-language
- F6.4: Public API
- Advanced analytics and ML-driven recommendations
