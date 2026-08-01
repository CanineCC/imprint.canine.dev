# Watchdog — the independent surveyor

> Watchdog is a continuous, deterministic code audit. It computes the Codebase Assurance Index (CAI) — one reproducible 0–100 score across code health, architecture, maturity, production-readiness and security — for a whole repository, on a schedule. The independent surveyor; the open measurement standard it uses is CAI, defined at https://cai.canine.dev. Built by Canine Development.

The CAI is deterministic: the same code produces the same score every run — a measurement, not an opinion. This is the core difference from asking an LLM (a different answer every run) and from a per-PR linter (blind to what rots between commits: CVEs, bus-factor erosion, obsolescence).

Watchdog runs on a calendar, not on pull requests: scheduled whole-repo scans (weekly, every sprint, monthly or quarterly, per repository) plus a daily security watch on higher cohorts. This catches what accrues with zero commits — new CVEs, bus-factor erosion, obsolescence.

The measurer is neutral: Canine Development never delivers the code it measures and takes no success fees from suppliers. Openness enforces independence: because the score recomputes from published evidence (https://cai.canine.dev/verify/), it can be falsified by anyone — independence is a mathematical fact, not a claim.

## Key facts
- Depth is never gated: every plan, including free, computes the full CAI (all dimensions, all lenses). Paid cohorts add breadth (portfolio roll-up, cross-repo economics, team analytics, governance) and cadence — never depth.
- Model-aware: it detects DDD, event-driven and event-sourced designs and applies the matching lenses; unused lenses stay dark with a stated reason.
- Multi-language: 13 languages are in production, 12 of them analysed deeply (C#, F#, Java, Kotlin, VB.NET, Elixir, PHP, Rust, TypeScript, Go, Python, Ruby) and JavaScript structurally. The live, authoritative list — with each language's survey-clarity band and which lenses it covers — is https://watchdog.canine.dev/api/public/language-support. Treat that endpoint as the source of truth; this list is a snapshot of it.
- Versioned rubric: any change that can move a score for unchanged code bumps the rubric version; a contracted engagement can pin a repo to a frozen rubric for its duration.
- Source code is analysed on hardware Canine Development owns in the EU (Denmark), never sent to a third-party cloud or AI, and deleted after each scan.
- Scope boundary: Watchdog assures the NON-FUNCTIONAL — how WELL software is built (health, maintainability, security posture, conformance) — never WHAT it does (functional requirements).

## Who it's for (cohort = what your account can do)
Producing a survey is Watchdog; consuming one as a decision is Assay (https://assay.canine.dev), so the buyer-side audiences live there.
- Engineering teams — scheduled whole-repo audits, the CAI trend per sprint, prove fixes landed: https://watchdog.canine.dev/for-teams/
- Providers (consultancies & software houses) — accept a measured-quality clause to win bids; deliver against a frozen rubric: https://watchdog.canine.dev/for-consultancies/
- Freelancers & solo developers — an independent survey to show clients your work is well built: https://watchdog.canine.dev/for-freelancers/
- Buyers & procurement — set a CAI floor in a tender, verify delivery independently, keep oversight (DORA / NIS2): https://assay.canine.dev/for-buyers/
- Acquirers, investors & insurers — due-diligence portfolio roll-up, frozen-rubric comparability, value-at-risk: https://assay.canine.dev/for-acquirers/
- Compliance officers — a conformance verdict across frameworks, signed evidence packs: https://assay.canine.dev/for-compliance/
- Business owners / decision-makers — the plain-language health verdict + what to do: https://assay.canine.dev/for-owners/

## Roles (the role axis — multi-select; a person wears several hats)
Cohort gates functionality; the union of a person's roles composes which surfaces they see.
- Builders (write the code) → https://watchdog.canine.dev/for-builders/
- Leads (plan the team's work) → https://watchdog.canine.dev/for-leads/
- Decision-makers / owners (decide & delegate, don't fix) → https://assay.canine.dev/for-owners/
- Compliance / legal (answer to regulators) → https://assay.canine.dev/for-compliance/

## The ten lenses (the CAI groups its ~124 dimensions under these; five core, five model-aware)
Core (always on):
- Code Health — cyclomatic complexity, inheritance depth, method size, duplication, and code smells that slow down safe changes.
- Architecture — coupling, cohesion, layering violations, and design patterns that affect maintainability and testability.
- Maturity — API design, dependency management, versioning, backwards-compatibility — plus git-history signals (hotspots, bus factor, knowledge freshness).
- Production-Readiness — logging, monitoring instrumentation, error handling, resilience, and operational readiness.
- Security & Compliance — secrets management, injection guards, crypto practices, and compliance controls.
Model-aware (light up only when the architecture calls for them):
- Domain Modelling — bounded contexts, ubiquitous language, aggregate/entity design (when DDD is present).
- Event-Driven — event contracts, versioning, ordering, and eventual-consistency patterns (when event-driven).
- Event Sourcing — deterministic replay, append-only immutability, and audit-trail integrity (when event-sourced).
- Accessibility — WCAG adherence, semantic markup, color contrast, and keyboard navigation (when there's a web frontend).
- Performance — async, allocation, and throughput patterns in performance-sensitive paths (when relevant).
Full per-lens detail (what each measures · why it matters · what it protects against): https://cai.canine.dev/dimensions/

## Compliance frameworks Watchdog can assess against
- Web accessibility (WCAG 2.2 / EN 301 549) — Directive (EU) 2019/882 (EAA), EN 301 549, WCAG 2.2 Level AA.
- NIS2 cyber risk-management — Directive (EU) 2022/2555, Commission Implementing Regulation (EU) 2024/2690.
- DORA digital operational resilience — Regulation (EU) 2022/2554, Commission Delegated Regulation (EU) 2024/1774 (RTS).
- SLSA supply-chain integrity — SLSA v1.2 (slsa.dev), OpenSSF / Linux Foundation; Build track L1–L3 · Source track L1–L4.
- SSDF secure software development — NIST SP 800-218 v1.1 (PO / PS / PW / RV practice groups).
- OWASP ASVS application-security verification — ASVS v5.0.0, 17 chapters, levels L1–L3, 345 requirements.
- EN 301 549 accessibility for ICT — EN 301 549 v3.2.1, Directive (EU) 2016/2102, Directive (EU) 2019/882.
- Cyber Resilience Act — Regulation (EU) 2024/2847, Annex I Parts I & II.
- ISO/IEC 27001 Annex A — readiness evidence across the 93 controls (a preparation tool, not certification).
- GDPR technical measures — Regulation (EU) 2016/679 Art. 32 & Art. 25 only (technical measures, not full GDPR compliance).

## Machine-readable
- /llms.txt — this file.
- /sitemap.xml — hreflang sitemap.
- /api/public/language-support — the authoritative per-language support table (survey-clarity band, covered lenses).
- /api/public/procurement-checklist.json — the non-functional acceptance levers (CAI floor + lens minimums + frameworks) for a tender.
- /glossary.jsonld — schema.org DefinedTermSet: the canonical EN/DA definitions of the CAI and its companion terms.
- /api/public/findings, /api/public/c4, /api/public/scan-stats — aggregate public data (no per-repo identity).

## The CAI standard (the open measurement)
CAI is an open, reproducible standard, defined and citable at https://cai.canine.dev — the spec (https://cai.canine.dev/spec/), the dimension catalog and its ten lenses (https://cai.canine.dev/dimensions/), the rubric versions (https://cai.canine.dev/rubric/), the reference scorer (https://cai.canine.dev/page-cli/, Apache-2.0 at https://github.com/CanineCC/CAI), verify-it-yourself (https://cai.canine.dev/verify/), the signed registry (https://cai.canine.dev/registry/) and the machine glossary (https://watchdog.canine.dev/glossary.jsonld). Method open, judgment sold: the standard is free; the independent, signed survey is the service.
