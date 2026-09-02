# Non-Functional Requirements - PrintGrid Platform

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

## Summary Matrix

| Category | Requirements | Critical | High | Medium | Low |
|----------|-------------|----------|------|--------|-----|
| Performance | 7 | 2 | 3 | 2 | 0 |
| Scalability | 3 | 0 | 1 | 2 | 0 |
| Reliability | 4 | 2 | 1 | 1 | 0 |
| Security | 8 | 6 | 1 | 0 | 0 |
| Usability | 5 | 0 | 3 | 1 | 1 |
| Maintainability | 4 | 0 | 3 | 1 | 0 |
| Compatibility | 3 | 0 | 2 | 1 | 0 |
| Legal/Compliance | 3 | 0 | 2 | 1 | 0 |
| Deployment | 3 | 0 | 3 | 0 | 0 |
| Testing | 3 | 1 | 2 | 0 | 0 |

**Total: 43 Non-Functional Requirements**

All requirements include **measurable targets**, **rationale**, and **testing approach**.
