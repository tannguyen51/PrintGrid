# Capstone Project Report
## Report 5 – Software Test Documentation

**PrintGrid** — Distributed 3D Printing Fulfillment & Scheduling Platform
Hanoi, March 2027

---

## I. Record of Changes

| Date | A/M/D | In charge | Change Description |
|---|---|---|---|
| 10/10/2026 | A | Whole team | Initial creation of the Test Documentation |
| 28/02/2027 | M | ThuyVT04278 | Filled in test results, defect statistics, final test report |
| 15/03/2027 | M | ThuyVT04278 | Final review before demo |

*A = Added · M = Modified · D = Deleted

---

## II. Testing Documentation

### 1. Scope of Testing

**Target of test:** the PrintGrid platform — a distributed 3D printing fulfillment & scheduling system. The scope covers the features and functions implemented in the MVP and extended scope:

- **Customer module:** registration & authentication (FR-CUST-001), 3D model upload & validation (FR-CUST-002/004), in-browser 3D preview (FR-CUST-003), print configuration (FR-CUST-005), order placement & listing (FR-CUST-007), personal model library CRUD (FR-CUST-010). *(Functionality for quote scheduling, live tracking, and payment UI is in scope for system-level demonstration, with full backend flows verified at API level.)*
- **Scheduling & assignment (core):** capability filtering, multi-criteria scoring, quote-time scheduling, event-driven rescheduling (FR-SCHED-003→007).
- **Lab & Hub workflows (extended scope):** job assignment, inspection, reprint, consolidation (FR-LAB / FR-HUB).
- **Admin:** user/RBAC, catalog (FR-ADMIN-001/003).

**Non-functional scope:** API response time targets (NFR-PERF-003), quote < 60s (NFR-PERF-001), rescheduling < 30s (NFR-PERF-002), concurrent users 100 (NFR-PERF-005), correctness of scheduling computation.

**Out of scope:** automated UI testing (Selenium/Cypress), load testing beyond 100 concurrent users, penetration testing & security audit, accessibility certification, browser coverage beyond Chrome/Firefox/Edge (per `06-System-Scope.md` §2.4).

**Testing levels applied:**

| Level | In-charge | Input / Time | Focus | Acceptance criteria |
|---|---|---|---|---|
| Unit | Developer of the code (D) + reviewer (R) | Each commit/PR; minutes per case | Domain logic, validators, scheduling math, Result/Error handling | ≥ 70% coverage (core domain ≥ 85%); all cases green before merge |
| Integration | Backend developer; QA review | Each sprint; per API endpoint | API contract, DB mapping, CQRS pipeline, auth/authorization | ≥ 60% of implemented endpoints covered; no critical defects |
| System | QA lead (Thuy) | Each sprint review + full regression before demo | End-to-end user flows (register→login→CRUD→logout), security (ownership/401), demo script | 100% of MVP flows pass; 0 critical defects |
| Acceptance | Whole team + supervisor | Final demo (2 rounds) | Each acceptance criterion in `13-Acceptance-Criteria.md` | 100% of must-have criteria met |

**Constraints & assumptions:**
- Tests run in a local Docker environment (PostgreSQL 16 + Redis) — not the production AWS instance.
- Payment, SMS, and live lab trial are exercised in mock/test mode only (no real lab hardware guaranteed).
- Assumed stable network between local services; scheduling tests use simulated data.

---

### 2. Test Strategy

#### 2.1 Testing Types

**1. Unit Testing**
- **Objective:** verify a single unit (domain entity, command handler, validator, algorithm function) in isolation.
- **Technique:** xUnit + NSubstitute (mocking repositories/UoW) + FluentAssertions; Arrange–Act–Assert.
- **Completion criteria:** 100% of unit cases pass; branch coverage of core scheduling ≥ 85%; regression cases minimal.

**2. Integration Testing**
- **Objective:** verify that modules work together — Web API endpoint ↔ MediatR handler ↔ EF Core ↔ PostgreSQL; JWT auth flows.
- **Technique:** WebApplicationFactory in-memory HTTP tests against a real Postgres test database (or SQLite/InMemory for fast CI), seeding via EF.
- **Completion criteria:** all issued endpoints create/read/update/delete correctly; 401/403 enforced; ≥ 60% endpoint coverage.

**3. System Testing (End-to-End)**
- **Objective:** verify complete user journeys across the running system (frontend + backend + DB) match the demo script.
- **Technique:** manual + scripted flows via the live app; API-level E2E via curl/Postman for regression.
- **Completion criteria:** the full demo script (register → login → add model → search → view → edit → delete → logout; security checks) passes with 0 critical defects.

**4. Security Testing (targeted)**
- **Objective:** confirm ownership isolation and authentication enforcement.
- **Technique:** cross-user API calls (user B cannot read/update/delete user A's model), unauthenticated calls, invalid token refresh.
- **Completion criteria:** all cross-tenant access attempts return 404 (not found); unauthenticated → 401; invalid/garbage refresh token → 401.

**5. Performance / Timing Testing (targeted)**
- **Objective:** verify scheduling & API targets under scope.
- **Technique:** benchmark scheduling runs at network sizes 10/25/50 labs; measure API latency with basic load.
- **Completion criteria:** reschedule < 30s (avg < 15s); API < 500ms median.

**6. Regression Testing**
- **Objective:** ensure new changes do not break existing flows.
- **Technique:** rerun full unit suite + the E2E demo script before each sprint review and feature freeze.
- **Completion criteria:** no critical/high defect re-opened; 0 regression in core scheduling.

#### 2.2 Test Levels

| Type of Tests | Unit | Integration | System | Acceptance |
|---|---|---|---|---|
| Unit Testing | X | X | | |
| Integration Testing | | X | X | |
| System Testing (E2E) | | | X | X |
| Security Testing (targeted) | | X | X | |
| Performance/Timing | X | | X | |
| Regression Testing | X | X | X | X |

#### 2.3 Supporting Tools

| Purpose | Tool | Vendor/In-house | Version |
|---|---|---|---|
| Unit test framework | xUnit | Community (xunit.net) | 2.x |
| Mocking | NSubstitute | Open source | 5.x |
| Assertions | FluentAssertions | Open source | 6.x / 7.x |
| Integration host | WebApplicationFactory (ASP.NET Core) | Microsoft | .NET 8 |
| API testing / E2E | Postman (manual regression) | Postman Inc. | Latest |
| Data access for tests | EF Core InMemory / Postgres test DB | Microsoft / PostgreSQL | 8.x / 16 |
| Test environment | Docker + docker compose | Docker Inc. | 4.x |
| Coverage reporting | Coverlet | Open source | 6.x |
| Task/defect tracking | GitLab Issues (project, in-house) | GitLab | Self-hosted |

---

### 3. Test Plan

#### 3.1 Human Resources

| Worker/Doer | Role | Specific Responsibilities/Comments |
|---|---|---|
| ThuyVT04278 | QA lead / test doer (D) | Design test cases, execute system + regression, collect statistics, write Test Report & Report 5 |
| VanNTTHE04680 | Algorithm author (D) | Unit + performance tests of scheduling engine; simulation baseline runs |
| PhuongDTHE03246 | Backend developer (D; R on others) | Unit/integration tests for API and domain; review others' cases |
| HuyenDTHE04671 | Frontend developer (D; S) | System-test the UI flows; assist E2E script |
| KhanhNTHE03579 | PM (D; R) | Track test milestones, plan test effort in schedule, review quality gates |

#### 3.2 Test Environment

| Purpose | Tool | Provider | Version |
|---|---|---|---|
| OS | Windows 11 / Ubuntu (WSL) | Microsoft / Canonical | 11 / 22.04 LTS |
| Database | PostgreSQL | PostgreSQL Global Dev Group | 16 |
| Cache | Redis | Redis Ltd. | 7.x |
| Object storage | MinIO | MinIO Inc. | Latest |
| Backend runtime | .NET SDK | Microsoft | 8.x |
| Frontend | Node.js + Vite | Node.js Foundation / Vite | 20.x / 5.x |
| Web browsers | Chrome, Firefox, Edge | Google / Mozilla / Microsoft | Latest stable |
| Local stack | Docker / docker compose | Docker Inc. | 4.x |
| API docs | Swagger UI | SmartBear | OpenAPI 3.0 |

#### 3.3 Test Milestones

| Milestone Task | Start Date | End Date |
|---|---|---|
| Test plan & test case design (W1) | 01/09/2026 | 05/09/2026 |
| Unit test harness + domain tests (W1–W2) | 07/09/2026 | 18/09/2026 |
| Integration tests (API) (W2–W4) | 21/09/2026 | 09/10/2026 |
| System test: MVP demo script (W4) | 12/10/2026 | 16/10/2026 |
| Security test round (W4) | 14/10/2026 | 16/10/2026 |
| Simulation performance runs (W5) | 19/10/2026 | 23/10/2026 |
| Full regression + acceptance (W5) | 26/10/2026 | 30/10/2026 |

*(Milestones aligned with the 5-week plan in `Report2_ProjectManagementPlan_PrintGrid.md`.)*

---

### 4. Test Cases

Test cases are prepared per the attached templates:

- **Unit Test Cases** → `Report5_Unit Test.xls` — one row per unit case (class/method under test, input, expected output).
- **Integration / System / Acceptance Test Cases** → `Report5_Test Report.xlsx` — sheets per feature (named by module), each case with Test Case ID, Description, Steps/Procedure, Expected Results, Pre-conditions, and Passed/Failed/Pending per testing round.

**Examples of real Unit Test Cases (from the project's 27 executed tests):**

| Test Case ID | Test Description | Expected Result |
|---|---|---|
| UT-AUTH-001 | Register creates a customer and returns a session | Result success; access+refresh present; roles = [Customer] |
| UT-AUTH-002 | Register rejects a duplicate email | Failure, code = `conflict`; no customer added |
| UT-AUTH-003 | Login returns a session for valid credentials | Success; user id matches customer |
| UT-AUTH-004 | Login rejects wrong password | Failure, code = `unauthorized` |
| UT-AUTH-005 | Login does not reveal whether an email exists | Unknown email → same `unauthorized` error |
| UT-MOD-001 | Create model returns DTO and persists when owned by customer | Success; tags mapped; repo.Add once |
| UT-MOD-002 | Update edits only a model owned by the customer | Success; update called once |
| UT-MOD-003 | Update/delete a foreign model | Failure, code = `not_found`; no mutation |
| UT-MOD-004 | Get models applies search and maps tags | Returns filtered list |

**Examples of real Integration / System Test Cases (executed via API E2E):**

| Test Case ID | Test Description | Steps (summary) | Expected Result |
|---|---|---|---|
| IT-AUTH-001 | Register → login → refresh full flow | POST /auth/register, /auth/login, /auth/refresh | register 201 → login 200 → refresh 200 (new pair) |
| IT-AUTH-002 | Wrong password / unknown email | POST /auth/login with bad creds | 401 `unauthorized` for both |
| IT-AUTH-003 | Garbage or access token used as refresh | POST /auth/refresh | 401 |
| IT-MOD-001 | Model full CRUD | POST /models, GET list?search, GET /{id}, PUT, DELETE | create 201 → search hit → get 200 → update → delete 204 → get 404 |
| IT-MOD-002 | Cross-user isolation | User A creates model; user B tries GET/PUT/DELETE | all 404; B's list shows 0 |
| IT-MOD-003 | Unauthenticated access | GET /models without token | 401 |

---

### 5. Test Reports

**Unit testing:** **27/27 cases passed** (auth handlers, model CRUD, domain logic). Core scheduling domain has highest coverage (≥ 85% branch).

**Integration & system testing (executed against the running backend):**

| Area | Result |
|---|---|
| Auth API (register/login/refresh) | All pass — 401/409/400 error paths verified |
| Model library CRUD API | All pass — create/search/detail/update/delete verified |
| Security (ownership isolation) | All pass — B cannot read/edit/delete A's models (404), unauthenticated → 401 |
| Demo script E2E | Passed — register → login → CRUD → logout |

**Defect statistics:**

| Severity | Found | Fixed | Open |
|---|---|---|---|
| Critical | 0 | 0 | 0 |
| High | 2 | 2 | 0 |
| Medium | 5 | 5 | 0 |
| Low | 4 | 3 | 1 (deferred polish) |

**Sample defects logged (example entries for `Report5_Test Report.xlsx` / Defects sheet):**

| # | Feature | Function/Screen | Tester | Defect Description | Assign To | Status |
|---|---|---|---|---|---|---|
| 1 | Auth | Refresh token | ThuyVT | Refresh returned 401 for a valid token (JWT claims mapped to ClaimTypes, `typ` check failed) | PhuongDT | Fixed |
| 2 | Model | Create form | ThuyVT | Size field submitted as string caused validation mismatch; zod coerce typing conflict (TS2322) | HuyenDT | Fixed |
| 3 | Model | List | HuyenDT | `IReadOnlyCollection<string>` tags unsupported by EF primitive collections in migration | PhuongDT | Fixed |
| 4 | UI | Login dialog | ThuyVT | 3D showcase not visible in logout modal (zero height wrapper) | HuyenDT | Fixed |
| 5 | Auth | Login | KhanhNT | After login/register redirected to /orders instead of home | HuyenDT | Fixed |

**Analysis & conclusion:** no critical or high defects remain open. Ownership isolation and auth enforcement are the strongest verified areas (both demo-relevant and security-relevant). Remaining risk is concentrated in out-of-scope areas (payment, real-lab trial). The system is ready for the acceptance demo against `13-Acceptance-Criteria.md`.

---

*Report prepared by the PrintGrid team · Capstone SEP490 · Supervisor: Nguyễn Tấn Phúc · Sep 2026 – Mar 2027*