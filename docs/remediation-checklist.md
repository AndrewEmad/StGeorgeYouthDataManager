# Remediation coverage and release gates

This is the finding-to-change-to-verification ledger for the approved complete plan. A checked finding has an implementation and a passing applicable local regression. Release checks that require the deployment environment remain separate and open. Credentials intentionally kept in appsettings are excluded.

## Fixed constraints

- [x] Keep SQL Server, the modular monolith, one church/team scope and two independently deployable Angular applications.
- [x] Keep ImageSharp **exactly 3.1.5** and all projects on **net8.0**. JPEG, PNG, WebP and GIF tests exercise that exact package. SDK 10 does not establish .NET 10 hosting compatibility; the runtime upgrade gate remains closed.
- [x] Patch supported .NET 8/EF/Identity dependencies to 8.0.31, remove legacy Identity 2.2, and patch the Excel packaging dependency. The full transitive NuGet audit now identifies only the explicitly pinned ImageSharp package. Its seven advisories remain visible; they have not been suppressed or represented as resolved.
- [x] Unify the Admin indigo/purple, dark slate/glass, Cairo typography and Arabic RTL design through `5.FrontEnd/shared/ui/styles/` and the shared npm workspace.

## Backend correctness and integrity

| Finding | Implemented change | Regression evidence |
|---|---|---|
| Inconsistent resource access and ownership responses | Shared resource policies cover student commands, lists/details, histories, photos, requests, tasks and reports/exports; forbidden ownership updates return 403 | `ApiTests`, `WorkflowTests`, `ActivityCorrectionTests` |
| Overexposed discovery / incomplete Secretary attendance | Discovery exposes ID/name/area/college/year only; Secretary gets minimal complete student rosters; servant attendance remains privileged | `ApiTests` |
| Ineligible assignees and visit participants | Validate active eligible users at submission and processing; participant history does not confer unrelated student access | `ApiTests`, `ReportTests` |
| Direct servant creation/import/deletion/reassignment | Privileged command policies; assigned users submit approval requests | `ApiTests` |
| Long-lived and unrevocable sessions | Hashed rotating refresh tokens, replay-family revocation, logout, current Identity stamp/account checks and bounded login throttling | `SessionAndTransactionTests`, `ApiTests` |
| Onboarding path exceptions and stale restrictions | Endpoint metadata, recomputed restrictions on issuance, validated self-profile including full name, server-confirmed replacement sessions | `ApiTests`; browser onboarding tests |
| Unchecked Identity results / concurrent role invariants | Checked operations enlisted in owned transactions; serialized single-Priest/last-active-Admin changes | `IdentityConcurrencyTests` |
| Partial business/approval/audit/notification writes | Top-level transaction ownership, nested savepoints, structured audits and persistent outbox committed together | `SessionAndTransactionTests`, `NotificationTests`, `ApprovalRetryTests` |
| Duplicate/stale approvals and unsafe retries | Row versions, pending uniqueness, processing revalidation, 409 ownership conflicts, idempotent approved addition-to-created-student link | `ApprovalRetryTests` |
| Missing withdrawal, processing fields and histories | Requester withdrawal, reasoned cancellations, complete submitted fields/processing metadata; personal and privileged processed histories | `ApprovalRetryTests`, `ApiTests` |
| Student field drift / omission-versus-clear ambiguity | Shared validation/limits/enums/reference checks; secondary phone and notes parity; presence-aware updates | `SessionAndTransactionTests`, `ImportAndArchiveTests`, `ApiTests` |
| Conflicting or invented birthdays | Full date is authoritative; valid month-only values survive; bounded migration does not invent dates or shift event timestamps | `MigrationAndBackfillTests`, `ReportTests` |
| Invalid and device-timezone-dependent dates | Validated calendar dates, UTC event timestamps and explicit Cairo conversion/boundaries | `ReportTests`, shared utility tests |
| Fragile CSV and poisoned persistence state | Quoted/multiline parsing, original row references, preview, visible rejects, date formats, read errors and cancellation; per-row savepoints | `ImportAndArchiveTests`, utility/browser import tests |
| Imports duplicated after lost responses | Durable operation/row fingerprint/outcome/created-ID records; successful retries return the same IDs | `ImportAndArchiveTests` |
| Missed shared-phone/name duplicates | Normalized indexed phone/name candidate review; explicit acknowledgement before import | `ImportAndArchiveTests` |
| Partial bulk assignment concealed | Per-student outcomes; successes commit and failed selections remain retryable | `ApiTests`; browser selection tests |
| Untraceable changes and editable history | Stable actor/target/correlation/time/reason and field changes; audited versioned correction/voiding preserves original activity | `ActivityCorrectionTests`, `SessionAndTransactionTests` |
| Archive/unassignment conflation and lost historical access | Separate unassignment/archive; cancel obsolete work; restore unassigned without reopening it; authorized archived history retained | `ImportAndArchiveTests`, `ReportTests`, `ApiTests` |
| Orphan work after reassignment/deactivation | Open/review tasks transfer to eligible owners or privileged unassigned-work queue; historic actors remain unchanged | `WorkflowTests`, `ImportAndArchiveTests` |
| Unsafe photo decoding/replacement and hidden partial failures | Byte/pixel/frame limits, orientation/metadata handling, required avatar frame only, versioned safe files, checked metadata save, narrow authorized metadata reads, upload-only UI retry | `PhotoTests`, `PhotoPersistenceTests`; browser photo retry |

## Attendance, tasks and reporting

| Finding | Implemented change | Regression evidence |
|---|---|---|
| Positive-only attendance cannot prove absence | Draft/Finalized/Cancelled dated sessions snapshot students, servant attendees and ownership; absence uses finalized known eligibility only | `WorkflowTests`, `AttendanceReportTests` |
| Duplicate marks / incorrect last attendance after correction | Idempotent marking/unmarking, audited correction and atomic reconciliation; finalized/cancelled controls and concurrency | `WorkflowTests` |
| Unknown legacy eligibility treated as complete | Legacy positives retain unknown coverage; explicit privileged review preserves positives and never invents historical owners | `ImportAndArchiveTests`, `MigrationAndBackfillTests` |
| Hard-coded meeting/report weeks | Versioned configurable Friday-default recurrence and report-week settings; cancelled meetings excluded | `WorkflowTests`, `ReportTests`; browser meeting dialogs |
| Scheduled dates mistaken for explicit work | Call/visit tasks with source, owner, due date and state transitions; completion plus activity in one transaction | `WorkflowTests` |
| Legacy dates inflate overdue reminders | Idempotent source-linked PendingReview backfill, excluded until explicitly activated | `MigrationAndBackfillTests`, `NotificationTests` |
| Hidden actionable workload | Assigned/open/due/overdue queues, distinct due students, never-attempted and unassigned dashboard metrics | SQL report tests; browser task/dashboard routes |
| Mismatched report/filter/export scope | One scoped SQL report provider, validated IDs/page/size/sort/date ranges, full-population totals, stable ID ordering | `ReportTests`, `AttendanceReportTests` |
| Date ends exclude the last day / inconsistent clocks | One request clock, Cairo inclusive starts/exclusive next-day ends, next-Friday week boundary; export labels declare creation/event semantics | `ReportTests`, utility tests |
| Name-based/current-owner activity attribution | Stable historical recorder/participant IDs; distinct servant credit, global visits counted once, legacy visit fallback and archived companion retention | `ReportTests` |
| Attempts conflated with success / invalid coverage | Separate never attempted, no recent attempts and no recent success; non-voided outcomes, deduplicated same filtered roster, null denominator rendered — | `ReportTests` |
| Incomplete report suite | Attendance history/rates/streaks/totals/trends; task states/owners; contact outcomes/coverage; assignments/unassigned/inactive owners; approval age/turnaround/outcomes; missing fields/duplicate candidates | `ReportTests`, `AttendanceReportTests`; all report routes |
| Incomplete/unsafe exports and unreadable print | Full filtered bounded XLSX/CSV, Arabic labels/actors/companions, native Excel dates/text phones, formula-safe CSV and single BOM; RTL print with period/scope/repeated headers | `ReportTests`, shared utility tests; browser report controls |

## Shared frontend and architecture

| Finding | Implemented change | Verification |
|---|---|---|
| Competing theme definitions and duplicated primitives | Shared tokens/components/utilities; page styles compose the same primitives; obsolete reachable replacements removed, dead install styles deleted | Two production builds; route/dialog/accessibility/visual suites |
| Responsive clipping, RTL and long-content issues | Shrinkable layouts/logical spacing/safe areas, wrapping controls, labelled table scroll/mobile rows, LTR contact isolation, header compensation | Eight-width route/dialog suites; landscape and text reflow |
| Inaccessible overlays and controls | Root-hosted dialog, focus containment/restoration, Escape/scroll handling, semantic labels/sort, reduced motion, contrast, visible focus and 44px controls | Three-browser keyboard/Axe/dialog suites |
| Stale requests and concealed errors | Cancel superseded searches; debounce/query scopes; loading/empty/failed/offline/saving distinctions; themed retryable form errors | Browser route states and failed-save tests |
| Lost selection and repeated submissions | Explicit page scope, indeterminate selection, scope-change notice, per-request busy guards, failed bulk selections retained | Browser cross-page attendance/bulk tests |
| Phone/date/participant model mismatches | One Cairo/date/phone/CSV utility layer, explicit country-code preservation, participant `servantName`, paged histories and selectors | Shared utility tests; browser phone actions; SQL histories |
| Messaging scales by loading whole rosters | Compact paged assigned recipients, locked/error-aware loading, accurate composer-open feedback | Browser messaging dialogs |
| Session leakage between apps/tabs or stale account work | Namespaced safe storage, same-app synchronization, coordinated refresh, account-change cleanup, API-only tokens, centralized 401/replacement/onboarding | Browser session/onboarding suites; API auth tests |
| N+1 histories/servants/attendance/role reads | Bounded SQL projections/aggregates/paging and bulk lookup; caller-scoped overlap sharing never caches completed report results | SQL translation/scope/cancellation tests; load benchmark |
| Unmeasured indexes / heavy attendance aggregation | Measured SQL plans, additive contact indexes and attendance roster columnstore/ownership covering index, early person filters and narrow aggregate/hash join, bounded concurrent read sharing | Benchmark results retained; execution plans reproducible with the benchmark tool |
| Unbounded initial bundles | Lazy routes and 500 kB CI error budgets; Admin 359.36 kB, Servant 354.03 kB | Production build |
| Unnecessary framework/security dependencies | Legacy Identity removed, .NET 8 compatible patches, canonical frontend lock with zero audit findings; ImageSharp exception documented | Release build, SQL tests, full dependency audits |

Frontend behavior rows are verified across 336 unique browser cases after targeted reruns. The broad 309-case run had one Firefox install-help fixture timing failure; waiting for the actual populated dashboard fixed the setup, and all eight affected Firefox widths passed. No application failure remains unresolved. Case results are retained in `docs/verification-results.json` and visual baselines under `5.FrontEnd/e2e/snapshots/`. Raw logs, screenshots of test runs, caches, disposable backups and generated publication files are disposable; the reusable tools regenerate them when needed. Cleanup is prepared in `tools/Clear-GeneratedFiles.ps1`, but automatic approval review blocked execution, so these files have not been deleted.

## Notifications and delivery

- [x] Durable outbox events, occurrences, per-device attempts/atomic claims, retries, worker checkpoint and bounded provider concurrency; SQL notification tests cover commit/partial retry/midnight/PendingReview/paused queued messages.
- [x] Catch up seven calendar days per batch and continue from the durable checkpoint until current; edited/paused/deleted schedules suppress obsolete queued deliveries.
- [x] Validate schedule days/time and at least one day; edit/pause/resume, inactive-user handling and invalid-token cleanup. Provider timeout defaults to 15 seconds, configurable within 5–30 seconds; failures remain visible.
- [x] Both PWA workers use base-relative app scopes/navigation and real `logo.png` icons; tokens attach only to API requests and private API responses bypass the static service worker cache.
- [x] Final packaged three-browser PWA registration/offline/subpath/update/draft-preservation and icon regression after the last worker/icon change.
- [x] Development/staging/production client configuration; one combined frontend-only Pages deployment job. Per the updated user preference, this workflow only installs build dependencies, builds/packages the apps and publishes to Pages; tests, audits, browser installation and backend operations are excluded. Verification and optional schema scripts are run separately by operators. Automatic migrations and missing role/admin initialization run by default before serving requests in development and production; isolated tests explicitly opt out.
- [x] Data exports rename plus old-route redirect, bounded idempotent backfill/preflight, readiness and error/slow-query/worker/delivery/migration metrics.
- [x] Database plus persistent-photo backup/restore tooling; actual isolated BACKUP/VERIFYONLY/RESTORE/MOVE/hash/decode regression passes.

## Final local verification

- [x] 41 SQL Server integration cases pass with zero failures/skips after restoring default startup migrations (result counters retained in `docs/verification-results.json`). `StartupMigrationTests` verifies fresh development/production startup, repeat-start preservation of existing data and explicit test opt-out. Each run uses generated disposable databases.
- [x] EF 8.0.31 reports no pending model changes; both idempotent controlled migration scripts generate successfully.
- [x] Both production builds and four meaningful shared utility tests pass. Frontend audit has zero findings; backend transitive audit leaves only the explicitly pinned ImageSharp advisories.
- [x] Twelve expanded dialog/import/failed-save smoke tests pass across Chromium/Firefox/WebKit; twelve controlled loading baselines captured successfully.
- [x] All-route/all-dialog/login/onboarding/session/selection/accessibility/visual/print coverage across three browsers and eight widths: 336 unique cases pass after the targeted Firefox setup correction (`docs/verification-results.json`).
- [x] Final packaged PWA suite and combined entry page: three PWA cases and 24 entry-page cases pass. Production repackaging is complete: all 97 service-worker asset hashes match, fixture API strings are absent, and each app has exactly one current stylesheet.
- [x] Final isolated 20-user reference-data benchmark passes every target, including twenty distinct area/page filters and assignment owners; browser/build work was stopped. Lists p95 54.7 ms; standard HTTP reports at most 458.8 ms; mixed-filter attendance 186.9 ms; mixed-owner attendance 56.7 ms. Provider queries remain bounded (1–13, with zero additional queries for callers sharing an overlapping identical attendance read).
- [x] Actual frontend Pages deployment succeeded for commit `924a7289db1261be1a898e3d59b318343c32a3f1` ([workflow run](https://github.com/AndrewEmad/StGeorgeYouthDataManager/actions/runs/38223633509)). The landing page and both `/StGeorgeYouthDataManager/admin/` and `/StGeorgeYouthDataManager/servant/` return HTTP 200. All 97 published asset hashes match; manifests and Angular/Firebase worker paths are correct. Both RTL login pages render in Chromium without runtime errors. This verifies the anonymous frontend deployment; authenticated/backend/provider workflows remain separate. Results are retained in `docs/verification-results.json`.

## Church wording and navigation revision (2026-10-12)

- [x] Replace runtime Arabic student terminology across both applications, report/export labels, API photo-save responses and new reminders. Source scans find no remaining singular/plural student wording; both clients and the API build successfully. Internal IDs/contracts and historical records remain intact.
- [ ] Close the navigation/dialog revision after the fresh Firefox verification gate passes. Implementation is complete: status is a labelled small header icon; the desktop sidebar is a compact 240px sticky column; scrolling stays within the navigation; obsolete overlay/spacing rules are removed; dialog/import close controls share a themed 44px Material icon button; icon slots remain stable during font loading; negative table margins and legacy page spacing are corrected; native selects use the shared dark theme with RTL-aware arrows. Chromium completed 66 functional cases without reported failures; WebKit passed all 66, including eight widths, landscape, enlarged text, keyboard/focus, accessibility, print and selection checks. Sixteen additional native-select keyboard/reflow checks across both engines and eight widths pass after the final select styling. Forty Chromium/WebKit visual cases also pass, refreshing 48 baseline images for populated/empty/failed/loading/validation/saving states. The Windows Firefox browser also stalls/fails during isolated session setup/teardown. Experimental rendering overrides were removed. The earlier 336-case evidence is historical and does not certify this fresh revision. See `churchUiRevision` in `docs/verification-results.json` for the remaining gate. These changes have not been published.

## Performance evidence

The benchmark section of `docs/verification-results.json` records SQL Server 17.0.4025.3 Express (compatibility 170), Windows x64, .NET 8.0.31, 32,452 MiB available host RAM and 20 logical processors. Each operation has 60 measured samples across 20 concurrent callers after warm-up. The dataset has 3,000 students, 50 servants, 100,000 calls, 25,000 visits and 104 weeks / 317,200 roster entries; HTTP verification adds twenty independently authenticated Manager accounts. All standard queries and mixed area/page/owner cases pass their p95 limits. Raw execution plans were inspected during remediation and can be removed with the generated artifacts; the benchmark tool regenerates them. The actual SQL edition's CPU/resource limits still apply; twenty host processors do not imply twenty Express execution cores.

| Operation | Provider p95 ms | Authenticated HTTP p95 ms | Target ms |
|---|---:|---:|---:|
| list | 45.7 | 54.7 | 500 |
| contact | 547.6 | 458.8 | 1000 |
| workload | 203.4 | 168.1 | 1000 |
| workload-owners | 435.8 | 355.3 | 1000 |
| data-quality | 139.2 | 138.8 | 1000 |
| attendance | 187.3 | 188 | 1000 |
| follow-up | 85.6 | 85.3 | 1000 |
| manager | 360 | 341.6 | 1000 |

The HTTP measurements include ASP.NET authorization and serialization using TestServer, not external network/TLS/IIS latency. Different-filter attendance originally exceeded the target; early filtered-person seeks and the measured ownership covering index fixed the bottleneck without caching completed reports.

## Deployment gates that remain open

These require the actual staging/production environment and cannot be closed by mock UI tests or a disposable local database.

- [ ] Inspect real legacy data, review preflight/backfill outcomes, activate reviewed tasks/rosters and enable their KPIs only after approval by the data owner.
- [ ] Exercise the intended IIS/container host, .NET 8 runtime, TLS/CORS, SQL migration permissions, durable photo volume, readiness and automatic startup migrations.
- [ ] Verify real FCM registration/rotation/logout, provider delivery, foreground/click behavior and both deployed worker scopes on the actual origin.
- [ ] Restore a representative real database/photo backup onto a separate staging target and exercise the restored API workflows.
- [ ] Repeat performance measurements on the documented deployment resources through real TCP/TLS/hosting. Local HTTP measurements include ASP.NET authentication and serialization but use TestServer transport.
- [ ] Consider .NET 10 only after exact ImageSharp 3.1.5 ASP.NET/EF/Identity/auth/migration/image/hosting tests pass there. Keeping .NET 8 fulfills the current compatibility constraint.
