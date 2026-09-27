# Non-Functional Requirements - PrintGrid Platform

## 0. Requirement Matrix (submission format)

16 core NFRs selected per Step-11 guideline (8–15±); each row carries the four measurable elements
(metric · threshold · measurement condition · method, packed in Acceptance Criteria), its own Req ID
matching the detailed blocks below (§1–§11), and maps 1-to-1 to the 12 bullets of register item d)
(P-69→P-80). Rows marked ★new have their detailed block in §11; everything not in this matrix is an
engineering standard (not part of the Report 5 test plan).

| Req ID | Type | Requirement Description | Category | Priority | Source | Acceptance Criteria | Status | Notes |
|--------|------|-------------------------|----------|----------|--------|---------------------|--------|-------|
| NFR-PERF-001 | NFR | Quoting runs asynchronously; preliminary estimate and committed quote returned within bounded times | Performance | Must | P-69 | Preliminary ≤ 5 s; committed quote ≤ 60 s (p95); staging, meshes ≤ 50 MB, 30 concurrent sessions; k6/JMeter script attached to Report 5 | Approved | Block §1 |
| NFR-PERF-002 | NFR | Rescheduling always completes within a hard time budget and keeps a feasible fallback | Performance | Must | P-70 · BR-SCHED-005 | Repair ≤ 30 s hard / ≤ 15 s mean; fallback schedule committed at timeout; load: 50 labs / 300 machines via UC-027 harness | Approved (B4, 23/09) | Block §1 |
| NFR-PERF-003 | NFR | Interactive API stays responsive under normal load | Performance | Should | supports P-69 | p95 ≤ 2 s under realistic traffic profile; k6 on staging | Approved | Block §1 |
| NFR-SCALE-001 | NFR | Network data model and algorithms scale without architectural change | Scalability | Must | P-79 | Functional at 50 labs / 300 machines / 1,000 orders-month / 500 active jobs; verified through NFR-PERF-001/002 and NFR-REL-004 runs at that configuration | Approved | Block §2 |
| NFR-REL-002 | NFR | Zero loss of committed orders, assignments and customer models | Reliability | Must | P-72 (data correctness) | Kill process mid-transaction ⇒ no corrupted records; backup/restore drill monthly with 0 loss on committed entities | Approved | Block §3 |
| NFR-REL-004 | NFR | Machine double-booking is impossible by construction | Consistency | Must | P-72 | 0 overlapping placements with N concurrent workers on one machine queue; race tests + DB constraints | Approved | Block §3 |
| NFR-REL-005 ★new | NFR | A job is never assigned to a machine that cannot physically produce it | Correctness | Must | P-71 · BR-ASSIGN-001 | 0 violations on the "deliberately infeasible" job suite, executed independently of the scoring logic | Specified | Block §11 · enforced by FR-SCHED-003 |
| NFR-REL-006 ★new | NFR | Committed delivery dates change only under approved conditions | Stability/Trust | Must | P-73 · B16 | Committed-vs-final date diff = 0 outside the approved list: (1) customer-requested change (2) force majeure (3) deadline unrecoverable after 2 reprints; each change notified and approved | Approved (B16, 23/09) | Block §11 · test on reschedule audit dataset |
| NFR-SEC-007 | NFR | Model files accessible only within the assigned job, logged, never in bulk | Security/IP | Must | P-75 · P-18 · BR-ACCESS-001..004 | 0 access outside job window (+4 h grace, B15 pending); 0 bulk downloads; 100 % accesses logged; adversarial suite (Practical bullet 11) | Approved · ⚠ B15 | Block §4 |
| NFR-SEC-009 ★new | NFR | Real-money payments keep card data off the platform and reconcile every transaction | Security/Payment | Must | P-25 · decision 23/09 · BR-PAY-005/006 | 0 PAN/CVV/expiry in DB or logs (token + txn id only); 100 % invalid-signature webhooks rejected; daily reconciliation: 0 unexplained diffs; 0 double refunds (idempotency key) | Approved (23/09) | Block §11 · links FR-CUST-008, FR-ANAL-005, UC-028 |
| NFR-USE-001 | NFR | A first-time customer completes an order without training | Usability | Should | — | 5 of 5 untrained users finish registration→order in ≤ 10 min; moderated usability test | Approved | Block §5 |
| NFR-USE-006 ★new | NFR | Lab interface works at the machine: few taps, tablet-sized, tolerates bad wifi | Usability | Must | P-78 | State update reachable within ≤ 3 taps from queue (B13 pending); input survives 2-min connection loss; measured by scripted test at partner lab (lab secured 23/09) | Draft ⚠ B13 | Block §11 · supersedes generic USE-002 for shop-floor |
| NFR-LEGAL-002 | NFR | Assignments, overrides, refunds and quality decisions are fully auditable | Auditability | Must | P-76 | Any 50 randomly sampled decisions reconstructible with candidate set, scores and config version (FR-SCHED-009) | Approved | Block §8 |
| NFR-MAINT-005 ★new | NFR | Commercial parameters are tunable without redeployment | Configurability | Must | P-77 · BR-CONFIG-001..003 | Threshold/weight change effective ≤ 1 min, no service restart; quotes and assignments created earlier keep their frozen version | Specified | Block §11 · e2e through FR-ADMIN-002 |
| NFR-TEST-003 | NFR | Scheduler claims are measured against baselines, not asserted | Research/QA | Must | P-82 · BR-OPS-008 | ≥ 3 baselines on identical seeded workload; protocol version pinned; same seed ⇒ same result (nondeterministic runs quarantined) | Approved | Block §10 · executed on UC-027 |
| NFR-ACC-001 ★new | NFR | Slicer estimate accuracy is continuously measured and corrected | Accuracy | Should | P-74 · BR-ESTIM-005 | MAPE < 25 % per machine model and trending down after each calibration cycle; dashboard metric + evaluation-run export | Specified | Block §11 · requires partner-lab data (secured 23/09) |

**Coverage check vs register item d):** P-69→PERF-001 · P-70→PERF-002 · P-71→REL-005 · P-72→REL-002/004 ·
P-73→REL-006 · P-74→ACC-001 · P-75→SEC-007 · P-76→LEGAL-002 · P-77→MAINT-005 · P-78→USE-006 ·
P-79→SCALE-001 · P-80→(out of MVP: fair distribution, declared in Out of scope) — 11/12 covered, 1
deliberately excluded with reason.

**Engineering standards (blocks kept below §1–§10 for architecture review; NOT in Report 5 test plan):**
PERF-004..007 · SCALE-002/003 · REL-001/003 · SEC-001..006, 008 · USE-002..005 · MAINT-001..004 ·
COMPAT-001..003 · LEGAL-001/003 · DEPLOY-001..003 · TEST-001/002.


---

## 1. Performance Requirements (NFR-PERF)

### NFR-PERF-001: Quote Response Time
**Category:** Response Time  
**Priority:** Critical  
**Requirement:** Quote generation must provide preliminary estimate within 5 seconds, with full quote (including trial placement) within 60 seconds for 95% of requests.

**Metric:** Response time measured from upload complete to quote displayed  
**Target:** 
- Preliminary: <5s (95th percentile)
- Full quote: <60s (95th percentile)
**Rationale:** User tolerance for waiting; competitive advantage
**Testing:** Load testing with various model complexities

---

### NFR-PERF-002: Scheduling Computation Time
**Category:** Response Time  
**Priority:** Critical  
**Requirement:** Rescheduling operations must complete within 30 seconds to avoid blocking production.

**Metric:** Wall-clock time from event trigger to committed plan  
**Target:** <30s (hard limit), <15s (average)  
**Rationale:** Production cannot wait; operators need updated assignments  
**Testing:** Benchmark with varying network sizes

---

### NFR-PERF-003: API Response Time
**Category:** Response Time  
**Priority:** High  
**Requirement:** 95% of API requests must respond within 2 seconds under normal load.

**Metric:** Server response time (time to first byte)  
**Target:** <2s (95th percentile), <500ms (median)  
**Rationale:** Acceptable web application performance  
**Testing:** Load testing with expected traffic patterns

---

### NFR-PERF-004: Database Query Performance
**Category:** Response Time  
**Priority:** High  
**Requirement:** Database queries must execute within 100ms for simple queries, 500ms for complex reporting queries.

**Metric:** Query execution time in PostgreSQL  
**Target:** 
- Simple CRUD: <100ms
- Joins/aggregations: <500ms
- Analytics: <2s
**Rationale:** Supports overall response time targets  
**Testing:** Query analysis with EXPLAIN ANALYZE

---

### NFR-PERF-005: Concurrent User Support
**Category:** Scalability  
**Priority:** Medium  
**Requirement:** System must support at least 100 concurrent users without degradation.

**Metric:** Concurrent sessions with acceptable response times  
**Target:** 100 concurrent users, response times within NFR-PERF-003  
**Rationale:** Expected peak usage for MVP scope  
**Testing:** Load testing with JMeter or k6

---

### NFR-PERF-006: File Upload Speed
**Category:** Throughput  
**Priority:** Medium  
**Requirement:** File upload should support at least 5 Mbps upload speed without timeout.

**Metric:** Upload bandwidth and timeout threshold  
**Target:** 5 Mbps minimum, 50 MB file uploads reliably  
**Rationale:** Support typical STL file sizes  
**Testing:** Upload various file sizes, measure transfer rate

---

### NFR-PERF-007: Background Job Processing
**Category:** Throughput  
**Priority:** Medium  
**Requirement:** Hangfire background jobs should not accumulate; processing rate must exceed creation rate.

**Metric:** Job queue depth over time  
**Target:** Queue depth remains <50 jobs under normal load  
**Rationale:** Prevents job backlog and delays  
**Testing:** Monitor queue depth during load test

---

## 2. Scalability Requirements (NFR-SCALE)

### NFR-SCALE-001: Network Size Support
**Category:** Horizontal Scale  
**Priority:** High  
**Requirement:** System must handle 50 labs, 300 machines, 1000 orders/month without architectural changes.

**Metric:** System remains functional at scale  
**Target:** 
- 50 labs
- 300 machines
- 1000 orders/month
- 500 concurrent jobs
**Rationale:** Defined scope for capstone; demonstrates scalability  
**Testing:** Simulation at scale, database sizing

---

### NFR-SCALE-002: Data Growth
**Category:** Data Volume  
**Priority:** Medium  
**Requirement:** Database must handle at least 100,000 orders over 1 year without performance degradation.

**Metric:** Query performance with large datasets  
**Target:** Performance targets maintained with 100K orders  
**Rationale:** Long-term operational viability  
**Testing:** Database seeding with realistic volumes

---

### NFR-SCALE-003: File Storage Growth
**Category:** Storage  
**Priority:** Medium  
**Requirement:** MinIO storage must efficiently handle 10,000+ model files (estimated 500 GB total).

**Metric:** Storage capacity and retrieval time  
**Target:** 500 GB, retrieval <2s per file  
**Rationale:** Supports order volume over 1 year  
**Testing:** Storage capacity test

---

## 3. Reliability Requirements (NFR-REL)

### NFR-REL-001: System Uptime
**Category:** Availability  
**Priority:** High  
**Requirement:** System must achieve 99% uptime during business hours (excluding planned maintenance).

**Metric:** Uptime percentage = (total_time - downtime) / total_time  
**Target:** 99% (allows ~7 hours downtime/month)  
**Rationale:** Business continuity; production depends on system  
**Testing:** Monitor uptime in production

---

### NFR-REL-002: Data Durability
**Category:** Data Integrity  
**Priority:** Critical  
**Requirement:** No data loss for committed orders, assignments, or customer models.

**Metric:** Data loss incidents  
**Target:** Zero data loss  
**Rationale:** Legal and business critical  
**Testing:** Database backup/restore testing, transaction rollback tests

---

### NFR-REL-003: Graceful Degradation
**Category:** Fault Tolerance  
**Priority:** Medium  
**Requirement:** System must continue core operations when non-critical services fail (e.g., email notifications).

**Metric:** Core workflows functional during partial outage  
**Target:** Quote, order, assignment, tracking remain operational  
**Rationale:** Resilience to external service failures  
**Testing:** Chaos engineering - disable external services

---

### NFR-REL-004: Transaction Consistency
**Category:** Data Integrity  
**Priority:** Critical  
**Requirement:** All database transactions must be ACID-compliant; no double-booking of machines.

**Metric:** Transaction rollback on failure, no data anomalies  
**Target:** 100% transaction consistency  
**Rationale:** Scheduling correctness depends on data integrity  
**Testing:** Concurrent transaction testing, race condition checks

---

## 4. Security Requirements (NFR-SEC)

### NFR-SEC-001: Authentication
**Category:** Access Control  
**Priority:** Critical  
**Requirement:** All users must authenticate before accessing protected resources.

**Metric:** Unauthenticated access blocked  
**Target:** 100% enforcement  
**Rationale:** Protects sensitive data and operations  
**Testing:** Penetration testing, automated security scans

---

### NFR-SEC-002: Password Security
**Category:** Authentication  
**Priority:** Critical  
**Requirement:** Passwords must be hashed using bcrypt or Argon2 with appropriate cost factor.

**Metric:** No plaintext passwords in database  
**Target:** 100% hashed passwords  
**Rationale:** Industry standard security practice  
**Testing:** Database inspection, security audit

---

### NFR-SEC-003: HTTPS/TLS Encryption
**Category:** Data Protection  
**Priority:** Critical  
**Requirement:** All client-server communication must use HTTPS with TLS 1.2 or higher.

**Metric:** No unencrypted HTTP traffic  
**Target:** 100% encrypted traffic  
**Rationale:** Protects data in transit, including payment info  
**Testing:** SSL Labs testing, network traffic inspection

---

### NFR-SEC-004: Role-Based Access Control (RBAC)
**Category:** Authorization  
**Priority:** Critical  
**Requirement:** Users must only access resources and perform actions permitted by their role.

**Metric:** Authorization checks enforced  
**Target:** 100% RBAC enforcement  
**Rationale:** Principle of least privilege  
**Testing:** Role-based test scenarios, privilege escalation attempts

---

### NFR-SEC-005: SQL Injection Prevention
**Category:** Input Validation  
**Priority:** Critical  
**Requirement:** All database queries must use parameterized statements; no string concatenation of user input.

**Metric:** No SQL injection vulnerabilities  
**Target:** Zero vulnerabilities  
**Rationale:** OWASP Top 10  
**Testing:** Static analysis, penetration testing

---

### NFR-SEC-006: XSS Prevention
**Category:** Output Encoding  
**Priority:** Critical  
**Requirement:** All user-generated content must be sanitized/escaped before display.

**Metric:** No XSS vulnerabilities  
**Target:** Zero vulnerabilities  
**Rationale:** OWASP Top 10  
**Testing:** XSS scanning, manual testing

---

### NFR-SEC-007: File Access Control
**Category:** Data Protection  
**Priority:** Critical  
**Requirement:** Customer model files must be accessible only to assigned labs during job window, with all access logged.

**Metric:** Unauthorized access blocked, 100% audit trail  
**Target:** Zero unauthorized access  
**Rationale:** IP protection business requirement  
**Testing:** Access control testing, audit log verification

---

### NFR-SEC-008: Session Management
**Category:** Authentication  
**Priority:** High  
**Requirement:** Sessions must timeout after 30 minutes of inactivity; secure session cookies (HttpOnly, Secure, SameSite).

**Metric:** Session security configuration  
**Target:** Proper configuration enforced  
**Rationale:** Reduces session hijacking risk  
**Testing:** Cookie inspection, session timeout verification

---

## 5. Usability Requirements (NFR-USE)

### NFR-USE-001: Learning Curve
**Category:** Learnability  
**Priority:** High  
**Requirement:** New users should complete first order within 10 minutes without training.

**Metric:** Task completion time for first-time users  
**Target:** <10 minutes from registration to order placed  
**Rationale:** Reduces barrier to adoption  
**Testing:** User testing with target personas

---

### NFR-USE-002: Mobile Responsiveness
**Category:** Accessibility  
**Priority:** High  
**Requirement:** All customer and lab operator interfaces must be usable on tablets (768px width minimum).

**Metric:** Layout and functionality on mobile devices  
**Target:** Full functionality on tablets, critical functions on phones  
**Rationale:** Lab operators use tablets at machines  
**Testing:** Responsive design testing, real device testing

---

### NFR-USE-003: Error Messages
**Category:** Error Handling  
**Priority:** Medium  
**Requirement:** Error messages must be clear, specific, and actionable (not technical jargon).

**Metric:** Error message clarity rated by users  
**Target:** 80% of users understand error and know next step  
**Rationale:** Reduces support burden, improves UX  
**Testing:** User testing, content review

---

### NFR-USE-004: Accessibility
**Category:** Inclusive Design  
**Priority:** Low (MVP), High (Future)  
**Requirement:** Interface should follow WCAG 2.1 Level AA guidelines where feasible.

**Metric:** WCAG compliance  
**Target:** Awareness and best effort for MVP; Level AA for production  
**Rationale:** Inclusive design; potential regulatory requirement  
**Testing:** Accessibility audit tools, screen reader testing

---

### NFR-USE-005: Internationalization Ready
**Category:** Localization  
**Priority:** Low (MVP)  
**Requirement:** UI strings should be externalized to support future localization.

**Metric:** Hard-coded strings vs. externalized  
**Target:** All user-facing strings in resource files  
**Rationale:** Enables future expansion  
**Testing:** Code review

---

## 6. Maintainability Requirements (NFR-MAINT)

### NFR-MAINT-001: Code Documentation
**Category:** Documentation  
**Priority:** Medium  
**Requirement:** All public APIs and complex algorithms must have clear documentation.

**Metric:** Documentation coverage  
**Target:** 100% public APIs, all scheduling algorithms  
**Rationale:** Enables knowledge transfer and maintenance  
**Testing:** Documentation review

---

### NFR-MAINT-002: Logging
**Category:** Observability  
**Priority:** High  
**Requirement:** System must log all errors, warnings, and critical business events with context.

**Metric:** Log coverage and quality  
**Target:** All exceptions logged, business events traceable  
**Rationale:** Debugging and operational visibility  
**Testing:** Log review, exception scenario testing

---

### NFR-MAINT-003: Configuration Externalization
**Category:** Deployment  
**Priority:** High  
**Requirement:** Environment-specific configuration must be externalized (not hard-coded).

**Metric:** Hard-coded config vs. externalized  
**Target:** Zero hard-coded environment config  
**Rationale:** Supports dev/test/prod environments  
**Testing:** Code review, deployment testing

---

### NFR-MAINT-004: Modular Architecture
**Category:** Design  
**Priority:** High  
**Requirement:** System must follow modular monolith with clear module boundaries.

**Metric:** Dependency analysis, coupling metrics  
**Target:** Low coupling between modules, high cohesion within  
**Rationale:** Maintainability, potential future microservices migration  
**Testing:** Architecture review, static analysis

---

## 7. Compatibility Requirements (NFR-COMPAT)

### NFR-COMPAT-001: Browser Support
**Category:** Client Compatibility  
**Priority:** High  
**Requirement:** Must support latest versions of Chrome, Firefox, Edge, Safari.

**Metric:** Functional testing across browsers  
**Target:** 100% functionality on supported browsers  
**Rationale:** Covers 95%+ of users  
**Testing:** Cross-browser testing

---

### NFR-COMPAT-002: Database Compatibility
**Category:** Server Compatibility  
**Priority:** High  
**Requirement:** Must work with PostgreSQL 14+.

**Metric:** Database version compatibility  
**Target:** PostgreSQL 14, 15, 16  
**Rationale:** Stable, modern PostgreSQL versions  
**Testing:** Testing on multiple versions

---

### NFR-COMPAT-003: Operating System
**Category:** Server Compatibility  
**Priority:** Medium  
**Requirement:** Backend must run on Linux (Ubuntu 22.04+) and Windows Server 2019+.

**Metric:** Deployment success  
**Target:** Successful deployment and operation  
**Rationale:** Docker ensures portability  
**Testing:** Deployment testing on both OSes

---

## 8. Legal and Compliance Requirements (NFR-LEGAL)

### NFR-LEGAL-001: Data Privacy
**Category:** Privacy  
**Priority:** High  
**Requirement:** System must allow users to export and delete their personal data (GDPR-inspired).

**Metric:** Data export and deletion capabilities  
**Target:** Complete data export, thorough deletion  
**Rationale:** Privacy best practices, potential GDPR applicability  
**Testing:** Data export/deletion testing

---

### NFR-LEGAL-002: Audit Trail
**Category:** Compliance  
**Priority:** High  
**Requirement:** All financial transactions, assignments, and quality decisions must be auditable.

**Metric:** Audit log completeness  
**Target:** 100% coverage of critical events  
**Rationale:** Dispute resolution, compliance  
**Testing:** Audit log review

---

### NFR-LEGAL-003: Terms of Service Acceptance
**Category:** Legal  
**Priority:** Medium  
**Requirement:** Users must explicitly accept terms before using platform.

**Metric:** Acceptance recorded  
**Target:** 100% users accepted current terms  
**Rationale:** Legal protection  
**Testing:** Registration flow testing

---

## 9. Deployment Requirements (NFR-DEPLOY)

### NFR-DEPLOY-001: Containerization
**Category:** Deployment  
**Priority:** High  
**Requirement:** All components must be Docker-containerized.

**Metric:** Services running in containers  
**Target:** 100% containerized  
**Rationale:** Environment consistency, easy deployment  
**Testing:** Docker Compose deployment

---

### NFR-DEPLOY-002: Environment Parity
**Category:** Deployment  
**Priority:** High  
**Requirement:** Development, testing, and production environments must use same stack.

**Metric:** Stack consistency  
**Target:** Identical stack versions  
**Rationale:** "Works on my machine" prevention  
**Testing:** Environment comparison

---

### NFR-DEPLOY-003: Database Migration
**Category:** Deployment  
**Priority:** High  
**Requirement:** Database schema changes must be version-controlled and automated via migrations.

**Metric:** Migration automation  
**Target:** Zero manual SQL scripts  
**Rationale:** Repeatability, audibility  
**Testing:** Migration up/down testing

---

## 10. Testing Requirements (NFR-TEST)

### NFR-TEST-001: Unit Test Coverage
**Category:** Quality  
**Priority:** High  
**Requirement:** Critical business logic must have unit test coverage >80%.

**Metric:** Code coverage percentage  
**Target:** >80% for domain and application layers  
**Rationale:** Ensures correctness, enables refactoring  
**Testing:** Coverage analysis tools

---

### NFR-TEST-002: Integration Testing
**Category:** Quality  
**Priority:** High  
**Requirement:** All API endpoints must have integration tests.

**Metric:** Endpoint test coverage  
**Target:** 100% of public endpoints  
**Rationale:** Verifies module integration  
**Testing:** Integration test suite execution

---

### NFR-TEST-003: Simulation Testing
**Category:** Quality  
**Priority:** Critical  
**Requirement:** Scheduling algorithms must be validated against simulation with multiple baselines.

**Metric:** Simulation results documented  
**Target:** Comparison against 2+ baselines  
**Rationale:** Core academic contribution validation  
**Testing:** Simulation framework execution

---

## 11. Detailed blocks — core NFRs created by the v1.1 / 23-09 sync

### NFR-REL-005: Feasibility Correctness
**Category:** Correctness · **Priority:** Critical · **Requirement:** The system must never assign a job to a machine that cannot physically produce it (build volume, tolerance, technology, material).
**Metric:** count of infeasible assignments · **Target:** 0 · **Condition:** adversarial suite of jobs deliberately infeasible per machine, run independently of the scoring component
**Rationale:** register calls this a correctness requirement, not quality-of-result; one infeasible assignment wastes a full print window and breaks the trust chain. **Tests:** integration suite + UC-027 stress runs. **Traces:** P-71 · BR-ASSIGN-001 · FR-SCHED-003 · NFR-PERF-002 co-tested.

### NFR-REL-006: Stability of Committed Promises
**Category:** Trust/Stability · **Priority:** Critical · **Requirement:** A committed delivery date may change only under the approved list: (1) customer-requested change, (2) force majeure, (3) deadline unrecoverable after two reprints. Every change is notified and re-approved before commit.
**Metric:** diff between committed and final dates outside the list · **Target:** 0
**Condition:** full reschedule history on staging dataset · **Tests:** audit diff query + notification check per event. **Traces:** P-73 · B16 decision 23/09 · BR-SCHED-008, BR-NOTIFY-002 · FR-SCHED-007, FR-ANAL-004.

### NFR-SEC-009: Payment Data Protection & Reconciliation
**Category:** Security/Payment · **Priority:** Critical · **Requirement:** Real-money payments must keep card/wallet credentials off the platform (hosted checkout; token + transaction id only) and every transaction must be reconcilable daily.
**Metric:** PAN/CVV fields in DB/logs; invalid-signature webhook acceptance; daily reconciliation diff; double-refund count · **Target:** 0 / 0 / 0 unexplained / 0
**Condition:** DB + log inspection, webhook replay, T-1 report on test-settlement data
**Tests:** security review + reconciliation drill. **Traces:** P-25 · decision 23/09 · BR-PAY-005/006 · FR-CUST-008, FR-ANAL-005 · UC-001, UC-028.

### NFR-USE-006: Shop-Floor Usability
**Category:** Usability · **Priority:** High · **Requirement:** Lab operators must reach any job-state update within ≤ 3 taps from the queue on a tablet, and entered data must survive a 2-minute connectivity loss.
**Metric:** tap count; input loss events · **Target:** ≤ 3 taps (B13 pending confirmation); 0 lost inputs
**Condition:** scripted test at partner lab on real tablets with throttled network · **Traces:** P-78 · FR-LAB-005 · UC-005/015.

### NFR-MAINT-005: Configurability Without Redeployment
**Category:** Configurability · **Priority:** Critical · **Requirement:** Pricing parameters, score weights, scheduling limits and thresholds are edited through the admin UI, effective ≤ 1 minute, without service restart; existing records keep their frozen version.
**Metric:** time-to-effect; restart count; retro-application count · **Target:** ≤ 60 s / 0 / 0
**Tests:** e2e via FR-ADMIN-002 while traffic runs. **Traces:** P-77 · BR-CONFIG-001..003.

### NFR-ACC-001: Estimation Accuracy & Calibration
**Category:** Accuracy · **Priority:** High · **Requirement:** The gap between slicer estimates and actual print durations is measured per machine model (MAPE) and corrected through calibration factors applied to quoting and scheduling.
**Metric:** MAPE per machine model · **Target:** < 25 % and decreasing after each calibration cycle (BR-ESTIM-005)
**Condition:** partner-lab production data + simulator runs · **Traces:** P-74 · BR-ESTIM-002..006 · FR-SCHED-008 · UC-018 · depends on lab secured 23/09.

---

## Summary Matrix

| Category | Requirements | Critical | High | Medium | Low |
|----------|-------------|----------|------|--------|-----|
| Performance | 7 | 2 | 3 | 2 | 0 |
| Scalability | 3 | 0 | 1 | 2 | 0 |
| Reliability (incl. ★REL-005/006) | 6 | 4 | 1 | 1 | 0 |
| Security (incl. ★SEC-009) | 9 | 7 | 2 | 0 | 0 |
| Usability (incl. ★USE-006) | 6 | 0 | 4 | 1 | 1 |
| Maintainability (incl. ★MAINT-005) | 5 | 1 | 3 | 1 | 0 |
| Compatibility | 3 | 0 | 2 | 1 | 0 |
| Legal/Compliance | 3 | 0 | 2 | 1 | 0 |
| Deployment | 3 | 0 | 3 | 0 | 0 |
| Testing | 3 | 1 | 2 | 0 | 0 |
| Accuracy ★ | 1 | 0 | 1 | 0 | 0 |

**Total: 49 detailed blocks → 16 core NFRs (§0 matrix) + 33 engineering standards.**

Bản cũ 43 gạch đủ format 4 thành phần nhưng vượt chuẩn 8–15 và **lệch phách**: 5 ràng buộc của phiếu
(P-71/73/74/76/77) chưa có NFR nào. Mục 0 đã sửa cả hai chiều: **11/12 gạch d) của phiếu có NFR đo
được, 1 gạch (P-80 fair distribution) chủ trương loại khỏi MVP kèm lý do Out of scope**. §11 thêm 6
block chi tiết cho các ID mới để mọi dòng bảng §0 trỏ vào một block thật. **Constraints tách riêng**
ở `07-Assumptions-Constraints.md` — đúng chuẩn B11.

Mọi NFR chủ đều hình dung được **một phép kiểm cụ thể ở Report 5** — điều kiện "xong" của Bước 11.
