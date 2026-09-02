# Assumptions and Constraints - PrintGrid Platform

## 1. Business Assumptions

### A1.1 Market and Demand Assumptions

**A-BIZ-001: Sufficient Demand Exists**
- **Assumption**: There is sufficient demand for 3D printing services to justify a network platform
- **Validation**: Market research shows growing 3D printing service market
- **Risk**: Low (industry trend supports this)
- **Impact if False**: Platform may not achieve critical mass

**A-BIZ-002: Labs Willing to Join**
- **Assumption**: Independent printing labs will join the platform and accept centralized assignment
- **Validation**: Partner lab discussions confirm interest
- **Risk**: Medium (some labs may prefer independence)
- **Impact if False**: Network too small to provide value

**A-BIZ-003: Customers Accept Single Platform**
- **Assumption**: Customers prefer one-stop-shop over contacting individual labs
- **Validation**: Common e-commerce pattern
- **Risk**: Low
- **Impact if False**: Customer acquisition difficult

**A-BIZ-004: Price Uniformity Acceptable**
- **Assumption**: Customers accept one price for one spec, even if internal costs vary
- **Validation**: Standard platform model (Uber, Airbnb)
- **Risk**: Low
- **Impact if False**: Pricing model needs redesign

**A-BIZ-005: Quality Control Centralization Accepted**
- **Assumption**: Labs accept hub-based quality inspection and fault attribution
- **Validation**: Industry standard for quality assurance
- **Risk**: Medium (labs may resist accountability)
- **Impact if False**: Quality disputes become unmanageable

---

### A1.2 Operational Assumptions

**A-OPS-001: Labs Report Honestly**
- **Assumption**: Labs generally report capabilities, inventory, and completion times honestly
- **Validation**: Performance tracking provides feedback loop
- **Risk**: Medium (incentive to over-promise)
- **Impact if False**: Assignment quality degrades; mitigation through performance ledger

**A-OPS-002: Hub Can Handle Throughput**
- **Assumption**: Single central hub can inspect and ship 500-1000 orders/month
- **Validation**: Based on e-commerce fulfillment center benchmarks
- **Risk**: Medium (operational execution risk)
- **Impact if False**: Hub becomes bottleneck; may need multiple hubs

**A-OPS-003: Operators Use System Correctly**
- **Assumption**: Lab operators will use the platform interface and report status accurately
- **Validation**: UI designed for ease of use
- **Risk**: Medium (training and change management needed)
- **Impact if False**: Data quality issues affect scheduling

**A-OPS-004: Network Density Sufficient**
- **Assumption**: 20-50 labs provide sufficient geographic coverage and capability diversity
- **Validation**: Based on problem statement
- **Risk**: Low (for MVP scope)
- **Impact if False**: Some orders may have no feasible lab

**A-OPS-005: Single Region Operation**
- **Assumption**: All labs and hub operate in one geographic region with reasonable transit times
- **Validation**: Scoped to avoid international shipping complexity
- **Risk**: Low (by design)
- **Impact if False**: Transit times and logistics become complex

---

### A1.3 Technical Assumptions

**A-TECH-001: Slicing Estimates Reasonable**
- **Assumption**: Open-source slicing engines provide estimates within 20-30% of actual print time
- **Validation**: Literature review + real lab trial
- **Risk**: Medium (known variance in practice)
- **Impact if False**: Calibration loop designed to compensate

**A-TECH-002: Internet Connectivity Available**
- **Assumption**: Labs have stable internet for platform access
- **Validation**: Standard expectation for modern businesses
- **Risk**: Low (offline mode nice-to-have, not required)
- **Impact if False**: Operator workflow disrupted

**A-TECH-003: Model Files Are Valid**
- **Assumption**: Most customer-uploaded files are valid STL/OBJ/3MF
- **Validation**: Validation service rejects invalid files
- **Risk**: Low (validation layer mitigates)
- **Impact if False**: Increased rejection rate at upload

**A-TECH-004: PostgreSQL Scales to Network Size**
- **Assumption**: PostgreSQL can handle 50 labs, 300 machines, 1000 orders/month
- **Validation**: Well within PostgreSQL capabilities
- **Risk**: Low
- **Impact if False**: Would need database optimization or sharding

**A-TECH-005: Scheduling Computes in Acceptable Time**
- **Assumption**: Heuristic scheduling algorithms can find good solutions within 30-60 seconds
- **Validation**: Algorithm design and time-budgeted approach
- **Risk**: Medium (NP-hard problem)
- **Impact if False**: May need more aggressive heuristics or longer wait times

---

### A1.4 User Behavior Assumptions

**A-USER-001: Customers Upload Reasonable Models**
- **Assumption**: Customer models are generally printable and appropriately sized
- **Validation**: Validation service provides feedback
- **Risk**: Low (guidance and validation mitigate)
- **Impact if False**: High rejection rate, customer frustration

**A-USER-002: Customers Accept Delivery Times**
- **Assumption**: Customers find 3-7 day delivery acceptable for 3D printed parts
- **Validation**: Industry norms for custom manufacturing
- **Risk**: Low
- **Impact if False**: May need express service tier

**A-USER-003: Labs Accept 80%+ Assignments**
- **Assumption**: When jobs are assigned after capability filtering, labs accept them most of the time
- **Validation**: Filtering should ensure feasibility
- **Risk**: Medium (late discoveries of infeasibility)
- **Impact if False**: High rejection rate reduces efficiency; acceptance rate tracked in performance ledger

**A-USER-004: Operators Report Actual Times**
- **Assumption**: Operators will take a few seconds to report actual print duration
- **Validation**: Incentivized by performance tracking
- **Risk**: Medium (compliance depends on workflow design)
- **Impact if False**: Calibration loop cannot function; remains uncalibrated

---

## 2. Business Constraints

### C2.1 Financial Constraints

**C-FIN-001: Capstone Budget Limitations**
- **Constraint**: No budget for cloud hosting, paid APIs, or premium services
- **Impact**: 
  - Use free tiers (Azure Student, AWS Free Tier)
  - Open-source software only
  - Local development environment
  - Mock payment processing (no real transactions)
- **Workaround**: Docker-based local deployment, free PostgreSQL, MinIO

**C-FIN-002: No Real Payment Processing**
- **Constraint**: Cannot process real credit card payments during capstone
- **Impact**: Payment gateway will be mocked or use Stripe test mode
- **Workaround**: Simulated payment success for demonstration

**C-FIN-003: Limited Partner Lab Compensation**
- **Constraint**: Cannot pay partner lab for trial participation
- **Impact**: Trial must be mutually beneficial (lab gets platform feedback)
- **Workaround**: Academic partnership, knowledge exchange

---

### C2.2 Time Constraints

**C-TIME-001: 6-Month Project Duration**
- **Constraint**: Project must be completed in 6 months (Sep 2026 - Mar 2027)
- **Impact**: 
  - Strict scope control
  - MVP-first approach
  - Features must be prioritized ruthlessly
- **Workaround**: Clear MVP vs. extended scope definition

**C-TIME-002: Team Member Availability**
- **Constraint**: 5 team members with other courses and commitments
- **Impact**: 
  - ~20 hours/week per person
  - ~100 person-hours/week total
  - ~2400 person-hours total project
- **Workaround**: Efficient task allocation, parallel work streams

**C-TIME-003: Academic Calendar**
- **Constraint**: Semester breaks, exam periods reduce working time
- **Impact**: 
  - Reduced productivity in Week 7-8 (midterms)
  - Holiday break disruption
- **Workaround**: Plan buffer time, frontload critical features

**C-TIME-004: Final Presentation Deadline**
- **Constraint**: Must demo by March 2027
- **Impact**: 
  - Feature freeze at Week 20
  - Weeks 21-24 for testing, documentation, demo prep
- **Workaround**: Progressive demonstration preparation

---

### C2.3 Resource Constraints

**C-RES-001: Limited Team Size**
- **Constraint**: Only 5 developers
- **Impact**: 
  - Cannot build everything
  - Specialization required (Algorithm, Backend, Frontend, Fullstack/QA)
  - Limited parallel work capacity
- **Workaround**: Clear work package ownership, modular architecture

**C-RES-002: Varied Skill Levels**
- **Constraint**: Team members have different experience levels
- **Impact**: 
  - Some features take longer
  - Knowledge sharing overhead
  - Quality variance
- **Workaround**: Pair programming, code reviews, mentoring

**C-RES-003: Single Advisor**
- **Constraint**: One faculty advisor for guidance
- **Impact**: 
  - Limited availability for questions
  - Bi-weekly meetings only
  - Self-directed problem solving required
- **Workaround**: Prepare questions in advance, use online resources

**C-RES-004: Limited Partner Lab Access**
- **Constraint**: Only 1-2 partner labs available for trial
- **Impact**: 
  - Cannot test at scale with real labs
  - Limited real-world validation
  - Simulation becomes critical
- **Workaround**: Build comprehensive simulation framework

---

### C2.4 Technical Constraints

**C-TECH-001: Modular Monolith Architecture**
- **Constraint**: Must use modular monolith, not microservices
- **Impact**: 
  - Simpler deployment
  - Easier development for small team
  - Potential future migration path
- **Benefit**: Reduced complexity, good fit for team size

**C-TECH-002: Technology Stack Fixed**
- **Constraint**: Backend (.NET 8), Frontend (React + TypeScript), Database (PostgreSQL)
- **Impact**: 
  - No experimentation with alternative stacks
  - Team must learn these specific technologies
- **Benefit**: Clear direction, no technology decision paralysis

**C-TECH-003: No Microservices**
- **Constraint**: Single deployable application (modular monolith)
- **Impact**: 
  - Cannot independently scale components
  - All modules deploy together
- **Benefit**: Simpler for capstone scope

**C-TECH-004: Local Development Environment**
- **Constraint**: Development primarily on local machines
- **Impact**: 
  - Docker-based setup required
  - Environment consistency challenges
  - No CI/CD pipeline initially
- **Workaround**: Docker Compose for environment parity

**C-TECH-005: No Real-Time Machine Integration**
- **Constraint**: Cannot integrate directly with 3D printers (no IoT)
- **Impact**: 
  - Status updates are manual (operator-driven)
  - Cannot auto-detect print completion
  - Cannot auto-collect print time data
- **Workaround**: Operator reports via UI

**C-TECH-006: Limited External API Access**
- **Constraint**: Cannot use expensive paid APIs
- **Impact**: 
  - Payment: Mock or test mode only
  - Shipping: No real carrier integration
  - SMS: No text notifications
  - Mapping: Use free tier or mock
- **Workaround**: Mock implementations, free-tier services

---

### C2.5 Data Constraints

**C-DATA-001: No Real Customer Data**
- **Constraint**: Cannot collect real customer PII during development
- **Impact**: 
  - Use synthetic test data
  - GDPR/privacy not fully tested
  - Anonymized demo data only
- **Workaround**: Generate realistic synthetic data

**C-DATA-002: Limited Historical Data**
- **Constraint**: No historical order/production data to start with
- **Impact**: 
  - Calibration requires accumulation time
  - Performance baselines from simulation only
  - ML not feasible (insufficient training data)
- **Workaround**: Simulation generates synthetic history

**C-DATA-003: Partner Lab Data Privacy**
- **Constraint**: Partner lab may not share all operational data
- **Impact**: 
  - Limited real-world validation data
  - Calibration based on trial only
- **Workaround**: Aggregate, anonymize where needed

---

### C2.6 Scope Constraints

**C-SCOPE-001: Single Geographic Region**
- **Constraint**: Platform designed for one region only
- **Impact**: 
  - Single hub location
  - No international shipping
  - No currency conversion
  - No multi-timezone handling beyond basic
- **Benefit**: Reduced complexity

**C-SCOPE-002: English Language Only (MVP)**
- **Constraint**: UI in English only for capstone
- **Impact**: 
  - Limited to English-speaking users
  - Internationalization deferred
- **Benefit**: Faster development

**C-SCOPE-003: Limited User Scale**
- **Constraint**: Designed for 50 labs, 1000 orders/month
- **Impact**: 
  - May not scale to enterprise level
  - Some optimizations not needed
- **Benefit**: Appropriate scope for capstone

**C-SCOPE-004: No Mobile Native Apps**
- **Constraint**: Web-only, no iOS/Android apps
- **Impact**: 
  - Mobile experience via responsive web
  - Cannot use native device features
- **Benefit**: Single codebase, faster development

---

### C2.7 Academic Constraints

**C-ACAD-001: Must Demonstrate Academic Rigor**
- **Constraint**: Project must show research, analysis, evaluation
- **Impact**: 
  - Documentation requirements high
  - Must compare against baselines
  - Must justify design decisions
- **Benefit**: Produces thorough understanding

**C-ACAD-002: Original Work Required**
- **Constraint**: Cannot use proprietary code or closed-source solutions for core contribution
- **Impact**: 
  - Scheduling algorithms must be original implementation
  - Cannot buy off-the-shelf solution
- **Benefit**: Genuine learning experience

**C-ACAD-003: Team Collaboration Required**
- **Constraint**: All 5 members must contribute meaningfully
- **Impact**: 
  - Work must be distributed
  - Individual contributions tracked
  - Cannot outsource work
- **Benefit**: Fair team-based learning

**C-ACAD-004: Public Presentation**
- **Constraint**: Must present and defend to examining panel
- **Impact**: 
  - Demonstration must be reliable
  - Team must understand all components
  - Cannot hide incomplete features
- **Benefit**: Forces completeness

---

## 3. Risk and Assumption Validation Plan

### 3.1 High-Risk Assumptions to Validate Early

| Assumption | Validation Method | Timeline | Owner |
|------------|------------------|----------|-------|
| A-TECH-005: Scheduling computes in time | Algorithm prototype + benchmark | Week 1-4 | WP2 (Algorithm) |
| A-TECH-001: Slicing estimates reasonable | Slicing engine integration + testing | Week 3-5 | WP3 (Backend) |
| A-OPS-003: Operators use system | Partner lab trial | Week 16-18 | WP5 (QA) |
| A-BIZ-002: Labs willing to join | Partner lab recruitment | Week 1-2 | WP1 (PM) |

### 3.2 Constraint Mitigation Strategies

| Constraint | Risk Level | Mitigation |
|------------|-----------|------------|
| C-TIME-001: 6-month duration | High | Aggressive scope control, MVP-first |
| C-RES-004: Limited partner labs | Medium | Comprehensive simulation framework |
| C-TECH-005: No real-time integration | Medium | Well-designed operator UI workflow |
| C-FIN-001: No budget | Low | Open-source stack, free tiers |

---

## 4. Assumption Dependencies

### 4.1 Critical Path Assumptions

If **A-TECH-005** (scheduling computes in time) is **FALSE**:
→ Core contribution at risk
→ Mitigation: Simplify algorithm, reduce network size, increase time budget

If **A-TECH-001** (slicing estimates reasonable) is **FALSE**:
→ Calibration becomes essential (move from extended to MVP)
→ Mitigation: Larger calibration factors, pessimistic estimates

If **A-BIZ-002** (labs willing to join) is **FALSE**:
→ Cannot demonstrate network effects
→ Mitigation: Simulation-only evaluation

If **A-OPS-003** (operators use system) is **FALSE**:
→ UI redesign required
→ Mitigation: User testing early, iterate on feedback

---

## 5. Constraints Acceptance

### 5.1 Non-Negotiable Constraints (Must Accept)

✅ 6-month timeline
✅ 5-person team
✅ No budget
✅ Technology stack
✅ Academic requirements
✅ Limited partner lab access

### 5.2 Negotiable Scope (Can Adjust)

🔄 Exact feature list (MVP vs. Extended)
🔄 Simulation scale (50 vs. 100 labs)
🔄 UI polish level
🔄 Number of baseline comparisons
🔄 Documentation depth beyond minimum

---

## 6. Success Criteria Under Constraints

Given the constraints, **success is defined as**:

✅ **MVP Functionality**: All core workflows working end-to-end
✅ **Scheduling Demonstrated**: Speculative quoting and rescheduling visible in demo
✅ **Evaluation Completed**: Simulation shows improvement over at least 2 baselines
✅ **Real-World Validation**: Partner lab trial provides calibration data
✅ **Documentation**: Complete requirements, design, implementation, and evaluation docs
✅ **Team Learning**: All members understand system and can defend design decisions

**Not Required:**
❌ Production-ready deployment
❌ Enterprise-scale performance
❌ Every possible feature
❌ Perfect UI polish
❌ Real customer traction

---

## 7. Assumption and Constraint Log

Throughout the project, maintain a living log:

```markdown
## Assumption Validation Log

| Date | Assumption | Status | Validation Result | Action |
|------|-----------|--------|-------------------|--------|
| 2026-09-15 | A-TECH-005 | Tested | ✅ Heuristic finds solution in 12s avg | Proceed |
| 2026-10-03 | A-TECH-001 | Tested | ⚠️ Estimates off by 35% | Enable calibration (moved to MVP) |
| 2026-11-12 | A-BIZ-002 | Validated | ✅ Partner lab confirmed | Proceed with trial |
```

---

## Summary

This project operates under:
- **38 explicit assumptions** across business, technical, and user domains
- **26 explicit constraints** related to time, resources, technology, and scope
- **Clear validation plans** for high-risk assumptions
- **Mitigation strategies** for major constraints

The team **accepts these constraints** and has designed the system scope and architecture accordingly. The **success criteria are achievable** within these boundaries.

**Key Takeaway:** We are **not building a production SaaS platform**. We are building a **capstone project demonstrating intelligent scheduling** for a distributed 3D printing network, with scope carefully bounded to be achievable in 6 months with 5 students.
