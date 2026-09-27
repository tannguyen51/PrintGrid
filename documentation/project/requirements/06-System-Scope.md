# System Scope - PrintGrid Platform

## 1. In Scope

### 1.1 Core Platform Capabilities

#### ✅ Customer Experience
- [x] Model upload and validation (STL, OBJ, 3MF formats)
- [x] In-browser 3D model preview
- [x] Print configuration interface
- [x] Automated quoting with capacity-based delivery dates
- [x] Secure checkout and payment processing
- [x] Real-time order tracking
- [x] Personal model library
- [x] Reprint/complaint requests

#### ✅ Lab Network Management
- [x] Lab registration and onboarding
- [x] Machine registry with specifications
- [x] Material inventory management
- [x] Job assignment with acceptance/rejection workflow
- [x] Machine-level schedule visualization
- [x] Production workflow for operators
- [x] Performance metrics and dashboard
- [x] Incident reporting system

#### ✅ Central Hub Operations
- [x] Batch receipt and reconciliation
- [x] Quality inspection with checklist-driven workflow
- [x] Defect classification and fault attribution
- [x] Reprint initiation and management
- [x] Order consolidation and fulfillment
- [x] Shipping integration

#### ✅ Scheduling and Assignment (Core Contribution)
- [x] Geometry analysis and slicing service
- [x] Network-wide uniform pricing engine
- [x] Capability-based filtering (hard constraints)
- [x] Multi-criteria scoring for assignment
- [x] Backward deadline scheduling
- [x] Material/color batch consolidation
- [x] **Speculative quote-time scheduling** ⭐
- [x] **Event-driven rescheduling** ⭐
- [x] **Print time estimation calibration** ⭐
- [x] Assignment decision audit logging

#### ✅ Quality and Performance
- [x] Central quality control workflow
- [x] Lab performance ledger
- [x] On-time delivery tracking
- [x] First-pass yield calculation
- [x] Performance-based assignment scoring

#### ✅ Platform Administration
- [x] User and role management (RBAC)
- [x] Configuration management without redeployment
- [x] Catalog management (materials, colors, technologies)
- [x] System monitoring dashboard
- [x] Audit trail and logging

#### ✅ Security and IP Protection
- [x] Time-limited model file access for labs
- [x] Model file access audit logging
- [x] Secure payment processing
- [x] Role-based access control
- [x] Data encryption at rest and in transit

---

### 1.2 Evaluation and Validation

#### ✅ Simulation Framework
- [x] Configurable order stream generator
- [x] Lab simulator with fault injection
- [x] Network simulator supporting 50+ labs
- [x] Performance metric collection
- [x] Baseline algorithm comparison

#### ✅ Algorithm Evaluation
- [x] Weighted tardiness measurement
- [x] On-time delivery rate analysis
- [x] Utilization and load distribution metrics
- [x] Comparison vs. random assignment baseline
- [x] Comparison vs. nearest-available-lab baseline

#### ✅ Real-World Trial
- [x] Partnership with one real printing lab
- [x] Actual vs. estimated print time comparison
- [x] Calibration factor validation
- [x] Operator interface usability testing

---

### 1.3 Technical Stack and Architecture

#### ✅ Backend
- [x] .NET 8 with C# (Modular Monolith + Clean Architecture)
- [x] PostgreSQL for primary data storage
- [x] Redis for caching and session management
- [x] Hangfire for background job processing
- [x] MinIO for object storage (model files, photos)
- [x] MediatR for domain events and CQRS
- [x] Entity Framework Core for ORM

#### ✅ Frontend
- [x] React 18 with TypeScript
- [x] Three.js for 3D model rendering
- [x] Material-UI or Ant Design component library
- [x] React Query for server state management
- [x] SignalR for real-time updates

#### ✅ Infrastructure
- [x] Docker containerization
- [x] Docker Compose for local development
- [x] Nginx as reverse proxy
- [x] SSL/TLS certificates for HTTPS

#### ✅ Development Practices
- [x] Git version control
- [x] Modular monolith architecture
- [x] Clean Architecture (Domain, Application, Infrastructure, Presentation)
- [x] Domain-Driven Design principles
- [x] Unit and integration testing
- [x] API documentation (Swagger/OpenAPI)

---

### 1.4 Documentation Deliverables

#### ✅ Requirements and Design
- [x] Context and problem analysis
- [x] Stakeholder analysis
- [x] Functional requirements specification
- [x] Non-functional requirements
- [x] System architecture document
- [x] Database schema (ERD)
- [x] API design specification

#### ✅ Implementation
- [x] Module design documents
- [x] Algorithm specifications
- [x] Code documentation
- [x] Deployment guide

#### ✅ Testing and Evaluation
- [x] Test plan and test cases
- [x] Simulation configuration and results
- [x] Algorithm evaluation report
- [x] Real lab trial findings

#### ✅ User Documentation
- [x] User guides (Customer, Lab, Hub, Operations, Admin)
- [x] Installation and setup guide
- [x] API documentation

---

## 2. Out of Scope

### 2.1 Explicitly Excluded Features

#### ❌ Payment Processing Backend Implementation
- **What's Excluded**: Building custom payment gateway, card processing
- **Rationale**: Use established payment provider (Stripe, PayPal) via API
- **In Scope Instead**: Integration with payment provider API

#### ❌ Custom Slicing Engine Development
- **What's Excluded**: Writing a 3D slicing engine from scratch
- **Rationale**: Complex, mature open-source solutions exist (CuraEngine, PrusaSlicer)
- **In Scope Instead**: Integration with existing slicing engine via CLI

#### ❌ Shipping Carrier Integration
- **What's Excluded**: Direct integration with FedEx, UPS, DHL APIs
- **Rationale**: Complex, region-specific, time-consuming
- **In Scope Instead**: Manual shipping label generation or mock integration

#### ❌ Mobile Native Applications
- **What's Excluded**: iOS and Android native apps
- **Rationale**: Web-based responsive design sufficient for MVP
- **In Scope Instead**: Mobile-responsive web application

#### ❌ Real-Time 3D Model Collaboration
- **What's Excluded**: Live multi-user 3D editing, comments on models
- **Rationale**: Not core to the scheduling problem
- **In Scope Instead**: Static 3D preview and upload

#### ❌ Blockchain-Based Provenance Tracking
- **What's Excluded**: Using blockchain for immutable audit trails
- **Rationale**: Over-engineered for problem; traditional database sufficient
- **In Scope Instead**: PostgreSQL audit tables with write-once patterns

#### ❌ Machine Learning Price Optimization
- **What's Excluded**: AI-driven dynamic pricing that adjusts based on demand
- **Rationale**: Complex, requires significant data, beyond capstone scope
- **In Scope Instead**: Parameterized formula with manual tuning

#### ❌ Customer Design Marketplace
- **What's Excluded**: Platform for customers to sell/buy STL files
- **Rationale**: Different business model, scope creep
- **In Scope Instead**: Personal model library only

#### ❌ AR/VR Model Preview
- **What's Excluded**: Augmented reality preview on customer's desk
- **Rationale**: Not core problem, technology complexity
- **In Scope Instead**: Browser-based 3D preview

#### ❌ IoT Machine Monitoring
- **What's Excluded**: Direct integration with 3D printers for real-time status
- **Rationale**: Requires hardware access, varies by manufacturer
- **In Scope Instead**: Manual status updates by operators

#### ❌ Automated Defect Detection via Computer Vision
- **What's Excluded**: AI-powered image analysis of printed parts
- **Rationale**: ML model training complexity, dataset requirements
- **In Scope Instead**: Manual inspection with photographic evidence

---

### 2.2 Post-MVP / Future Enhancements

#### 🔮 Phase 2 Considerations (Not in Capstone)

**Advanced Analytics:**
- Predictive demand forecasting
- Machine learning for assignment optimization
- Anomaly detection for quality issues
- Customer behavior analytics

**Extended Integrations:**
- CAD software plugins (Fusion 360, SolidWorks)
- E-commerce platform integrations (Shopify, WooCommerce)
- ERP system connectors
- Slack/Teams notifications

**Customer Features:**
- Social features (model sharing, community)
- Design consultation services
- Bulk order management dashboard
- Subscription pricing tiers

**Lab Features:**
- Multi-lab chain management (one owner, multiple locations)
- Advanced capacity planning tools
- Revenue forecasting
- Operator performance tracking

**Platform Features:**
- Multi-region deployment (US, EU, Asia)
- Multi-currency support
- Localization (multiple languages)
- White-label offerings for enterprise

**Technical:**
- Microservices migration (from modular monolith)
- Kubernetes orchestration
- Multi-cloud deployment
- GraphQL API

---

### 2.3 Network Scale Boundaries

#### ✅ Designed For:
- **50 labs** in network
- **200-300 machines** total
- **500-1000 orders per month**
- **Single geographic region** (hub within one country)

#### ❌ Not Designed For (Requires Architecture Changes):
- **500+ labs**: Would need microservices, distributed scheduling
- **10,000+ orders per month**: Would need horizontal scaling, message queues
- **Global distribution**: Would need multi-hub architecture, regional routing
- **Real-time streaming from machines**: Would need MQTT/IoT infrastructure

---

### 2.4 Quality and Testing Boundaries

#### ✅ In Scope:
- Unit tests for business logic
- Integration tests for API endpoints
- Manual testing of critical user workflows
- Simulation-based algorithm validation
- One real lab trial for calibration

#### ❌ Out of Scope:
- Automated UI testing (Selenium, Cypress) - time constraints
- Load testing beyond 100 concurrent users
- Penetration testing and security audit
- Accessibility compliance certification (WCAG)
- Browser compatibility beyond Chrome/Firefox/Edge

---

### 2.5 Data and Privacy Boundaries

#### ✅ In Scope:
- Basic GDPR compliance considerations (data export, deletion)
- Secure password storage (hashing)
- HTTPS/TLS encryption
- Basic audit logging

#### ❌ Out of Scope:
- Full GDPR compliance certification
- HIPAA compliance (not applicable)
- PCI DSS Level 1 compliance (payment provider handles this)
- SOC 2 certification
- Geographic data residency requirements

---

## 3. Scope Management Strategy

### 3.1 Minimum Viable Product (MVP) - Week 1-12

**Goal:** Working system demonstrating core scheduling contribution.

**Must Have:**
1. Customer can upload model, get quote, place order
2. Slicing analysis provides print time estimates
3. Speculative quote-time scheduling produces delivery date
4. Capability filtering prevents infeasible assignments
5. Multi-criteria scoring assigns jobs to labs
6. Lab operator workflow (accept, produce, report)
7. Hub quality control with fault attribution
8. Event-driven rescheduling handles failures
9. Simulation framework with baseline comparison
10. Basic UI for all user types

**Success Criteria:**
- Demo shows quote-time scheduling in action
- Rescheduling recovers from simulated failures
- Simulation shows performance vs. baselines
- All critical workflows functional

---

### 3.2 Extended Scope - Week 13-18

**Goal:** Polish and advanced features distinguishing strong result.

**Nice to Have (Priority Order):**
1. ⭐ Print time calibration loop (high academic value)
2. ⭐ Performance ledger feeding back into assignment (network improvement)
3. Model library and reorder functionality (user experience)
4. Manual intervention tools (operational realism)
5. SLA dashboards and reporting (business value)
6. Improved UI polish and mobile responsiveness

**Success Criteria:**
- Calibration shows improvement in accuracy over time
- Performance ledger demonstrably affects assignment
- UI feels professional and complete

---

### 3.3 Risk Mitigation: Scope Creep Prevention

**Warning Signs:**
- ⚠️ Building features not in requirements document
- ⚠️ Spending >20% of time on UI polish
- ⚠️ Adding "just one more" feature repeatedly
- ⚠️ Exploring new technologies not in stack
- ⚠️ Perfectionism on non-core modules

**Mitigation Actions:**
1. **Weekly scope review** in team meeting
2. **Feature freeze** at Week 14 (only bugs and refinement after)
3. **Protected core**: Scheduling and rescheduling algorithms are non-negotiable
4. **Time-boxing**: Each feature has max time budget, cut if exceeded
5. **Defer to Phase 2**: Maintain backlog of good ideas for "future work"

---

### 3.4 Scope Communication

**With Academic Supervisor:**
- Monthly progress reviews against this scope document
- Early warning if slipping on core features
- Approval required for scope changes

**Within Team:**
- This document is the contract
- Any feature not listed here requires team discussion
- Use "In Scope?" as default question for new ideas

**With Partner Lab (Trial):**
- Clear expectations: 1-2 week trial, specific data collection
- Not expecting full production deployment
- Focus on calibration and usability feedback

---

## 4. Success Criteria by Scope

### 4.1 Absolute Minimum (Project Pass Threshold)

**Must Demonstrate:**
- ✅ Working upload, quote, order flow
- ✅ Jobs assigned to labs based on capabilities
- ✅ Schedule produced and visualized
- ✅ Lab and hub workflows functional
- ✅ Simulation runs and produces metrics

**Academic Contribution:**
- ✅ Scheduling algorithm implemented (not just lead-time table)
- ✅ Comparison against at least one baseline
- ✅ Documentation of design decisions

---

### 4.2 Good Project (Expected Outcome)

**Must Demonstrate (Above + ):**
- ✅ Speculative quote-time scheduling working
- ✅ Event-driven rescheduling handles 3+ event types
- ✅ Quality control with fault attribution
- ✅ Simulation shows improvement over baselines
- ✅ Real lab trial validates estimation

**Academic Contribution:**
- ✅ Clear analysis of scheduling problem complexity
- ✅ Justified design decisions with trade-offs
- ✅ Meaningful evaluation with discussion

---

### 4.3 Excellent Project (Extended Scope)

**Must Demonstrate (Good + ):**
- ✅ Calibration loop shows improving accuracy
- ✅ Performance ledger influences assignment
- ✅ Manual override tools with audit trail
- ✅ Comprehensive simulation evaluation
- ✅ Professional UI across all modules
- ✅ Deployment-ready documentation

**Academic Contribution:**
- ✅ Novel insights from real lab trial
- ✅ Thorough evaluation of multiple baselines
- ✅ Discussion of network effects and fairness
- ✅ Contribution to open-source (potential)

---

## 5. Scope Dependencies

### 5.1 External Dependencies (Required)

**Slicing Engine:**
- Dependency: CuraEngine or PrusaSlicer CLI
- Risk: If unavailable, must mock slicing (high risk to core contribution)
- Mitigation: Test early, have backup engine

**Partner Lab:**
- Dependency: Access to one real lab for trial
- Risk: If unavailable, cannot validate calibration
- Mitigation: Identify and secure partnership in Week 1-2

**Payment Provider:**
- Dependency: Stripe or PayPal API access
- Risk: Low (well-documented, sandbox available)
- Mitigation: Can mock payments if needed

---

### 5.2 Internal Dependencies (Team Coordination)

**Backend API → Frontend:**
- Frontend blocked until API contracts defined
- Mitigation: Define OpenAPI spec early, use mocks

**Scheduling Engine → Simulation:**
- Simulation cannot evaluate until scheduler implemented
- Mitigation: Build simulation harness early with dummy scheduler

**Quality Module → Rescheduling:**
- Reprint triggered by quality failures
- Mitigation: Can test rescheduling with manual triggers initially

---

## 6. Scope Validation Checklist

Before implementing any feature, ask:

- [ ] Is it in the "In Scope" section of this document?
- [ ] Does it contribute to the core scheduling contribution?
- [ ] Is there time budget remaining for this feature?
- [ ] Is it required for MVP, or can it be deferred to extended scope?
- [ ] Have we validated the core features are solid first?

If answer to any is NO → **Do not implement, add to "Future Work" instead.**

---

## Summary

This project's scope is **bounded to be achievable in 6 months** while still delivering **meaningful academic contribution**. The scope focuses on:

1. ✅ **Core**: Capacity-aware scheduling with speculative quoting and dynamic replanning
2. ✅ **Essential**: Complete workflows for customer, lab, hub, operations
3. ✅ **Validation**: Simulation framework and real lab trial
4. ✅ **Extended**: Calibration, performance tracking, and polish

Everything else is **explicitly out of scope** or **Phase 2**.

**Remember:** The examining panel evaluates the **scheduling and rescheduling intelligence**, not the prettiness of the storefront. Protect the core contribution above all else.
