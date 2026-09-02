# PrintGrid Documentation

## Project Overview
**PrintGrid: Development of a Distributed 3D Printing Fulfillment and Scheduling Platform for a Network of Independent Printing Labs**

Capstone Project - Software Engineering  
Duration: September 2026 - March 2027  
Team: 5 members  
Supervisor: Nguyễn Tấn Phúc (phucnt40@fpt.edu.vn)

---

## 📚 Documentation Index

### 1. Project Foundation
- **[01-Context.md](01-Context.md)** - Industry background, core problem, and why this matters
- **[02-Stakeholders.md](02-Stakeholders.md)** - Who uses this system and what they need
- **[03-Problems-Challenges.md](03-Problems-Challenges.md)** - Detailed analysis of problems and technical challenges

### 2. Solution Design
- **[04-Proposed-Solutions.md](04-Proposed-Solutions.md)** - Complete solution architecture and approach
- **[05-Main-Features.md](05-Main-Features.md)** - Feature catalog for all user types
- **[06-System-Scope.md](06-System-Scope.md)** - What's in scope, what's out, and why

### 3. Requirements
- **[07-Assumptions-Constraints.md](07-Assumptions-Constraints.md)** - What we assume and what limits us
- **[08-Business-Rules.md](08-Business-Rules.md)** - 71 business rules governing the platform
- **[09-Functional-Requirements.md](09-Functional-Requirements.md)** - 44 functional requirements across all modules
- **[10-Non-Functional-Requirements.md](10-Non-Functional-Requirements.md)** - 43 performance, security, and quality requirements

### 4. Implementation (To Be Created)
- **11-User-Stories.md** - User stories with acceptance criteria
- **12-Use-Cases.md** - Detailed use case descriptions
- **13-Acceptance-Criteria.md** - Testing and acceptance criteria
- **14-System-Architecture.md** - Technical architecture and design
- **15-Module-Design.md** - Module-level design (Clean Architecture + DDD)
- **16-Database-ERD.md** - Database schema and entity relationships
- **17-API-Design.md** - REST API specifications

---

## 🎯 Quick Navigation by Role

### For Project Manager
Start here:
1. [Context](01-Context.md) - Understand the problem
2. [System Scope](06-System-Scope.md) - Know what we're building
3. [Assumptions & Constraints](07-Assumptions-Constraints.md) - Understand limitations

### For Algorithm Engineer (WP2)
Focus on:
1. [Problems - Technical Challenges](03-Problems-Challenges.md#3-technical-challenges)
2. [Proposed Solutions - Scheduling](04-Proposed-Solutions.md#25-deadline-backward-scheduling-with-batch-consolidation)
3. [Business Rules - Scheduling](08-Business-Rules.md#3-scheduling-rules-br-sched)
4. [Functional Requirements - Scheduling](09-Functional-Requirements.md#4-scheduling-and-assignment-module-fr-sched)

### For Backend Developer (WP3)
Focus on:
1. [Proposed Solutions - Core Components](04-Proposed-Solutions.md#2-core-solution-components)
2. [Functional Requirements - All Modules](09-Functional-Requirements.md)
3. [Non-Functional Requirements](10-Non-Functional-Requirements.md)
4. Architecture & Database docs (TBD)

### For Frontend Developer (WP4)
Focus on:
1. [Main Features - Customer & Operations](05-Main-Features.md)
2. [Functional Requirements - Customer/Analytics](09-Functional-Requirements.md)
3. [Non-Functional Requirements - Usability](10-Non-Functional-Requirements.md#5-usability-requirements-nfr-use)

### For QA Engineer (WP5)
Focus on:
1. [System Scope](06-System-Scope.md)
2. [Business Rules](08-Business-Rules.md)
3. [Functional Requirements](09-Functional-Requirements.md)
4. [Non-Functional Requirements - Testing](10-Non-Functional-Requirements.md#10-testing-requirements-nfr-test)

---

## 📊 Documentation Statistics

| Document | Size | Key Content |
|----------|------|-------------|
| Context | 9.4 KB | Problem analysis, market landscape |
| Stakeholders | 12.8 KB | 13 stakeholder groups, engagement strategy |
| Problems/Challenges | 17.5 KB | 35+ identified challenges with priorities |
| Proposed Solutions | 23.6 KB | 11 major solution components, algorithms |
| Main Features | 23.4 KB | 60+ features across 6 user types |
| System Scope | 16.7 KB | MVP vs Extended scope, boundaries |
| Assumptions & Constraints | 18.0 KB | 38 assumptions, 26 constraints |
| Business Rules | 32.1 KB | 71 rules across 12 domains |
| Functional Requirements | 16.5 KB | 44 requirements across 6 modules |
| Non-Functional Requirements | 16.0 KB | 43 requirements across 10 categories |

**Total: ~185 KB of comprehensive documentation**

---

## 🔑 Key Concepts

### Core Innovation
The platform's key differentiator is **capacity-aware scheduling**:
- Quotes delivery dates from **actual network capacity**, not lead-time tables
- **Speculative placement** at quote time ensures realistic promises
- **Event-driven rescheduling** handles production failures gracefully
- **Continuous calibration** improves accuracy over time

### Technology Stack
- **Backend:** .NET 8 (C#) with Modular Monolith + Clean Architecture
- **Frontend:** React 18 + TypeScript
- **Database:** PostgreSQL 14+
- **Cache/Queue:** Redis + Hangfire
- **Storage:** MinIO (S3-compatible)
- **Infrastructure:** Docker + Docker Compose

### Team Structure (5 members)
- **WP1:** Project Manager / Business Analyst
- **WP2:** Algorithm Engineer (Scheduling & Optimization)
- **WP3:** Backend Developer (Core Services)
- **WP4:** Frontend Developer (Customer & Operations UI)
- **WP5:** Full-stack Developer / QA Engineer (Lab/Hub Apps, Testing)

---

## 📋 Requirements Summary

### Functional Requirements: 44
- **Critical:** 18 (must have for MVP)
- **High:** 16 (should have for complete system)
- **Medium:** 10 (nice to have / extended scope)

### Non-Functional Requirements: 43
- **Performance:** 7 (response times, throughput)
- **Security:** 8 (authentication, encryption, access control)
- **Reliability:** 4 (uptime, data integrity)
- **Scalability:** 3 (50 labs, 300 machines, 1000 orders/month)
- **Others:** 21 (usability, maintainability, compatibility, etc.)

### Business Rules: 71
- **Critical:** 39 (must enforce in MVP)
- **High:** 23 (should enforce)
- **Medium:** 9 (extended scope)

---

## 🎓 Academic Contribution

This project demonstrates academic rigor through:

1. **Complex Problem:** NP-hard scheduling with multi-dimensional constraints
2. **Novel Approach:** Quote-time speculative scheduling + event-driven replanning
3. **Rigorous Evaluation:** Simulation framework with baseline comparisons
4. **Real-World Validation:** Partner lab trial for calibration
5. **Complete Documentation:** Requirements, design, implementation, evaluation

**Key Takeaway:** This is NOT just another e-commerce platform. The scheduling engine is the contribution, demonstrating intelligent distributed manufacturing coordination.

---

## 🚀 Getting Started

### For New Team Members
1. Read [Context](01-Context.md) (15 min)
2. Read [System Scope](06-System-Scope.md) (15 min)
3. Skim [Main Features](05-Main-Features.md) for your role (20 min)
4. Deep dive into requirements relevant to your work package

### For Development
1. Review technical stack in [Proposed Solutions](04-Proposed-Solutions.md)
2. Understand [Business Rules](08-Business-Rules.md) for your module
3. Reference [Functional Requirements](09-Functional-Requirements.md) during implementation
4. Test against [Non-Functional Requirements](10-Non-Functional-Requirements.md)

### For Evaluation
1. Understand problem from [Context](01-Context.md) and [Problems](03-Problems-Challenges.md)
2. Review solution approach in [Proposed Solutions](04-Proposed-Solutions.md)
3. Check implementation against [Requirements](09-Functional-Requirements.md)
4. Validate against simulation results and real lab trial

---

## 📝 Document Conventions

### Identifiers
- **BR-XXX-NNN:** Business Rule (e.g., BR-QUOTE-001)
- **FR-XXX-NNN:** Functional Requirement (e.g., FR-CUST-001)
- **NFR-XXX-NNN:** Non-Functional Requirement (e.g., NFR-PERF-001)
- **A-XXX-NNN:** Assumption (e.g., A-TECH-001)
- **C-XXX-NNN:** Constraint (e.g., C-TIME-001)

### Priority Levels
- ⭐ **Critical:** Must have for project success
- 🔴 **High:** Should have for complete system
- 🟡 **Medium:** Nice to have, extended scope
- 🟢 **Low:** Future enhancement

### Status Indicators
- ✅ In Scope / Completed
- 🔄 In Progress
- ❌ Out of Scope
- 🔮 Future Phase

---

## 🔄 Document Maintenance

**Version Control:** All documents are in Git for version tracking

**Review Schedule:**
- **Weekly:** Team reviews relevant sections
- **Bi-weekly:** Update with supervisor feedback
- **Monthly:** Comprehensive review and updates

**Change Process:**
1. Identify need for change
2. Discuss with team
3. Update document
4. Notify affected team members
5. Commit to Git with clear message

---

## 📞 Contact & Resources

**Supervisor:** Nguyễn Tấn Phúc  
**Email:** phucnt40@fpt.edu.vn

**Project Repository:** (TBD)  
**Project Duration:** Sep 2026 - Mar 2027

**Important Dates:**
- **Week 1-4:** Requirements finalization, environment setup
- **Week 5-12:** MVP development
- **Week 13-18:** Extended features, testing
- **Week 19-20:** Real lab trial
- **Week 21-24:** Documentation, demo preparation
- **March 2027:** Final presentation

---

## 🎯 Success Criteria

The project succeeds when it demonstrates:

✅ **Working MVP:** All core workflows functional  
✅ **Scheduling Intelligence:** Speculative quoting and dynamic rescheduling visible  
✅ **Evaluation Rigor:** Simulation shows improvement over baselines  
✅ **Real-World Validation:** Partner lab trial provides calibration data  
✅ **Complete Documentation:** This documentation set fully maintained  
✅ **Team Understanding:** All members can defend design decisions

**Remember:** We're building a capstone demonstrating intelligent scheduling, not a production SaaS platform. Scope is carefully bounded for 6-month completion with 5 students.

---

*Last Updated: September 1, 2026*  
*Documentation Version: 1.0*
