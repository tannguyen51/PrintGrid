# PrintGrid Overview — Presenter Script (English)

*Pairs with `PrintGrid-Overview-Slides.html` (11 slides, arrow keys). Total ≈ 8–9 minutes at a calm pace.
Stage directions in [brackets]; **bold** = the line to actually say out loud.*

---

## Slide 1 · Title — 20s

> Good morning. I'm Tân from team FA26SE249, and this is **PrintGrid** — a distributed 3D-printing fulfillment and scheduling platform for a network of independent printing labs, supervised by Mr. Nguyễn Tấn Phúc.
> In the next ten minutes: the context, the problems, and the complete functional and non-functional requirement set that answers them.

## Slide 2 · Context — 60s

> Start with the supply side. Around us — including our own university maker lab and a **confirmed partner workshop** — there are many small FFF and SLA labs. Each owns real machines with real idle time, and each runs its queue through chat apps.
> The demand side is students, researchers, small businesses: people who want a part printed **to a date they can trust**, at a price that doesn't change depending on which lab answers the message.
> Our thesis: treat the entire network as **one virtual factory** — uniform pricing, capacity-based delivery promises, one quality standard enforced at a central hub.
> [point at the diagram] This is the system boundary: PrintGrid is the black box in the middle. Nine external parties exchange information with it — customers, lab staff, hub staff, operations, administrator, and two external systems, our payment gateway and notification service. Every arrow across that box carries a named piece of business information, and we documented all twenty-four of them before designing a single screen.

## Slide 3 · Problems — 75s

> So what's actually broken? Five failures we can name.
> **One:** idle machines sit two buildings away from refused orders — capacity is fragmented and invisible.
> **Two:** delivery dates are guesses. Quotes promise "three to five days" from a static table — nobody looked at the real machine schedules. A promise you can't compute is a promise you'll break.
> **Three:** quality drifts with no accountability. When a part fails, there is no inspection standard and no fault attribution — it becomes he-said-she-said between lab, hub, and the customer's own model file.
> **Four:** every disruption — a lab that declines, a print that fails at ninety percent — is renegotiated manually, person to person. That costs hours of chat.
> **And five:** price depends on who asks which lab. That kills trust in a shared brand.
> Success for us is one sentence: **one real-money order travels from upload to delivered with zero manual dispatch, and a disruption mid-run is repaired in under thirty seconds.**

## Slide 4 · The four pillars — 60s

> Four mechanisms answer those five problems.
> **Network-uniform pricing:** one formula over versioned parameter sets. And the version is *frozen* onto the quote — if we change rates tomorrow, yesterday's quote still reproduces exactly. That's a testable promise, not a policy.
> **[point] This is our core contribution: speculative quote-time scheduling.** At the moment of quoting, the engine runs a *trial* placement against the real timelines of all machines, and returns the earliest feasible date — with a surcharge if you want it rushed. The delivery promise is computed, not looked up.
> **Hub-mediated quality:** every part passes a central hub — batch reconciliation, grade-bound checklists, defect taxonomy with fault attribution, and a reprint that inherits the original deadline.
> **Event-driven rescheduling:** decline, failure, breakdown, QC fault — each triggers a repair of the unstarted part of the plan within a thirty-second hard budget, with a feasible fallback always committed.
> One note: customers never learn *which* lab prints their part, and labs never see each other's prices. Uniformity here is enforced by architecture, not by rules on paper.

## Slide 5 · FR map — 40s

> Now the requirement inventory. Forty-nine functional requirements in six subsystems: thirteen for the customer, eight for the lab, six for the hub, ten for the scheduling engine — the starred group — seven for analytics and operations, five for administration. Six of these were added two weeks ago by our supervisor’s Review 1, which is exactly how the process is supposed to work.
> Two things we insist on: every requirement is **traced** back to a line in the approved proposal — forty-nine out of forty-nine in the traceability matrix; and every requirement is **prioritised** with MoSCoW, where Must is exactly the golden path plus its failure handling. Preview-3D and the model library are Could — cuttable, and we say so honestly.

## Slides 6–8 · The full FR lists — 90s total

> [don't read the tables — these three slides exist to prove completeness; speak over them:]
> These are all forty-nine items. Rather than read them, let me highlight what the list *refuses* to leave vague.
> **Slide 6** — the customer journey ends in **real money**: an SEPay QR code, and an order commits only when the verified webhook *and* the gateway's confirmation API agree. The browser redirect proves nothing — we designed the security assumption in from day one.
> **Slide 7** — the starred row is speculative placement again, and next to it the guard rails: hard feasibility filtering before any assignment, decision logs that can reconstruct every choice, and a hub that physically blocks packing until every item passed inspection.
> **Slide 8** — operations get overrides with reasons and audits, refunds with daily reconciliation, and administrators get one capability that matters commercially: change any price, weight or limit **live, in under a minute, with no redeployment** — while old quotes keep their frozen version.

## Slide 9 · NFR part 1 — 50s

> Functional requirements say *what*; non-functional requirements decide *whether we're believed*. Seventeen core items, and each one carries a number and a test — here are the first eight.
> Quoting: preliminary under five seconds, committed quote under sixty at p95 — measured with k6 on staging.
> Rescheduling: thirty seconds hard, fifteen mean, and if the optimizer can't improve in time, a **fallback schedule is still committed** — the system never leaves a hole.
> Scale: fifty labs, three hundred machines, a thousand orders per month without changing the architecture.
> And three zero-tolerance properties: zero lost orders, zero machine double-bookings — impossible *by construction*, enforced at the database level — and zero assignments to machines physically unable to print the job.
> Plus the trust rule: a committed date changes **only** under three approved conditions, each notified and approved by the customer.

## Slide 10 · NFR part 10–16 — 50s

> The second half. Security: model files are intellectual property — reachable only inside the assigned job window, fully logged, never in bulk. And for payments: no card data ever touches our database or logs.
> Usability, both sides: a first-time customer completes an order in under ten minutes **without training** — moderated test, five out of five users; and on the shop floor, the lab app is three taps to any state change and survives a two-minute Wi-Fi dropout, because it will be used *next to a running printer*, not at a desk.
> Auditability: any fifty sampled decisions must reconstruct fully, with candidates, scores and configuration version.
> And the research commitments — because this is a capstone, not just a product: scheduler claims are **measured against at least three baselines on identical, reproducible seeds**, and slicer accuracy is continuously tracked, with a target MAPE below twenty-five percent and trending down after every calibration cycle against the partner lab's real prints.

## Slide 11 · Closing — 40s

> To close the loop: one hundred four proposal statements extracted, one hundred three business rules, forty-nine functional requirements, seventeen measurable non-functional requirements, thirty-three use cases, forty-four user stories — one matrix wires them all together, and every information flow on the context diagram is handled by at least one use case.
> Three words for what we're defending: **traceable, falsifiable, and already building** — the API and event contracts are locked, the shared stack runs, and the golden-path sprint is underway.
> Thank you — questions welcome.

---

## Likely questions & short answers

| Q | A |
|---|---|
| Why a hub instead of shipping from labs? | Uniform QC, consolidation (one customer order can span several labs), and lab-identity hiding — the hub is what makes "one virtual factory" real instead of a marketplace. |
| Speculative placement at quote time — isn't that expensive? | Bounded by design: trial only, never commits; ≤60 s p95 is an NFR with a test; if no feasible placement exists, the quote is refused — an honest failure mode. |
| Payment: what if the webhook arrives twice or never? | Commit requires webhook + confirmation-API match, idempotent on transaction id; return URL is never proof; daily T-1 reconciliation catches missing/duplicate cases. |
| 30 s repair budget — measured how? | On the simulator harness (UC-027) at 50 labs/300 machines with fault injection, plus race tests for the no-double-booking constraint. |
| Why hide the lab from the customer? | The brand promise is uniform price and quality; exposing labs reintroduces per-lab negotiation and blame games, which is problem P3/P5 itself. |
| What's cut first if you slip? | The Coulds: 3D preview, model library, Gantt view — all visible in the SRS marked Should/Could so the scope is honest before anyone asks. |
