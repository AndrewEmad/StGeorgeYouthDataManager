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
- [x] Navigation/dialog verification is complete in the subsequent church-service report revision: all three browsers passed the fresh route/navigation/accessibility/print and visual checks. Earlier Firefox session failures were environmental; a new isolated one-worker run completed all 73 cases. See the final revision below and `churchServiceReportsRevision` in `docs/verification-results.json`. These changes have not been published.

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

## Admin navigation and church-service reports (2026-10-12)

This revision supersedes the earlier sidebar description. Existing historical verification/deployment entries above remain historical; the changes below have not been published.

| Finding / requirement | Change | Verification |
|---|---|---|
| Admin menu prioritizes data entry over oversight | Exactly 13 links in the approved order; statistics remains the default; one Reports link; small header status icon | Navigation checks at eight widths plus landscape and 200% text reflow |
| Sidebar has screen-edge gaps and rounded shell | Fixed flush 240px desktop panel; header/content reserve its width; scrollable links and fixed title; flush mobile drawer up to 320px | Bounds, no page overflow, last-link reachability, 44px close control, Escape and focus restoration |
| Reports scattered and hard to discover | One catalog, four groups, twenty described cards; old deep links retain functions; old hub redirects; verified obsolete hub sources removed | Catalog/deep-link route tests; production builds |
| No combined picture of a servant's service | `servant-service-summary`: own attendance/rate, current group, call/visit outcomes, distinct youths, current open/overdue tasks | SQL same-name, inactive/role-changed actor, shared/legacy visit and zero-activity fixtures; actor credits deduplicated, global visits counted once |
| Group attendance follows current assignment | `group-attendance` groups by the stored meeting owner, including an explicit unknown-owner group | SQL reassignment, cancelled meeting, unknown roster and empty-denominator cases |
| Youths needing pastoral attention not actionable | `pastoral-follow-up` shows trailing confirmed absences, last attendance/attempt/success, current owner, open work and reasons; explicit create-follow-up link | SQL period/history/contact fixtures; browser details/task dialog; no tasks created by report reads |
| Meetings omit servant attendance | Meeting report shows both student and servant eligible/present/rates and full filtered totals | SQL cancelled/unknown sessions and separate filter semantics |
| Summary drilldowns lose context | Person, current group, historical group attendance, own attendance, activity and tasks; applied filters retained in URL for refresh/back | Deep-link and single-fetch filter-persistence tests |
| Current roster includes archives | Current-roster boundary excludes archived youths; recorded historical activity remains available | SQL current assignment/archive and actor-history assertions |
| Screen, totals, export and print differ | Full filtered XLSX/CSV/print through the same provider; Arabic headings, native date cells, text phones, inclusive Cairo dates; internal IDs support links without displayed ID columns | API authorization/403, defaults, pagination, totals, dates and export parity; browser applied-filter print/export checks |
| Heavy overlapping and mixed-filter reads | SQL person-history seeks and full-population SQL totals/paging; overlap sharing without completed-result caching; stable ID ordering and cancellation | Eight latest report/concurrency regressions pass; final 20-user benchmark passes all new-report and distinct-filter/owner targets |
| Requested infrastructure must remain intact | No runtime/package/ImageSharp, model/migration, startup migration, credentials or Pages workflow changes | Git diff inspection; ImageSharp remains 3.1.5 and .NET remains 8 |

- [x] Full SQL Server suite: 48 passed, zero failures/skips. After the final query optimization, all eight affected calculation/API/concurrency regressions pass again.
- [x] Both production builds pass: Admin 363.25 kB, Servant 357.80 kB, below the 500 kB limit.
- [x] Broad Chromium and WebKit verification: 72 cases per engine across both apps, reachable routes/dialogs, eight widths, landscape, enlarged text, accessibility, print and visual states.
- [x] Final functional browser verification: broad Firefox run passes all 73 cases; the final new-report/drilldown/filter-persistence rerun passes 14 cases in each of Chromium and WebKit. The combined unique coverage is 73 cases per engine (219 across three engines). Both earlier and final runs are distinguished in `churchServiceReportsRevision` in `docs/verification-results.json`.
- [x] Final isolated benchmark: 3,000 youths, 50 servants, 100,000 calls, 25,000 visits, 104 weeks and 317,200 roster rows, 20 concurrent callers. Provider p95: summary 168.1 ms, groups 281.3 ms, pastoral 853.6 ms. Authenticated HTTP p95: summary 812.8 ms, groups 273.0 ms, pastoral 747.6 ms; distinct area/page pastoral queries 946.4 ms; distinct-owner pastoral queries 308.5 ms. Query counts remain bounded. Actual deployment latency/resources remain a separate rollout check.
- [x] Final visual captures: 18 new-report baselines across all three browsers at 390/1440px; desktop and portrait outputs inspected. One overlapping Firefox capture timed out, then passed in isolation; the broad 73-case run needed no retries.
- [ ] Publish the coordinated API and both clients, then verify actual authenticated routes and reports on the deployment origin. The Pages workflow remains frontend-only.

## Personal activity and consistent page gutters (2026-10-12)

- [x] Personal history supplies the signed-in actor explicitly. Unqualified API reads default to that actor for all five roles; privileged accounts no longer receive global activity from an unqualified personal-history request. Explicit authorized student/actor histories preserve existing scope, archived history and shared-visit participation without duplicate rows.
- [x] Add period, youth-name/notes search, outcome and active/voided filters to activity history. SQL filters apply identically to rows and full totals before stable date/ID paging. Cairo inclusive calendar boundaries, invalid ranges/enums/state, record-ID narrowing and 403 behavior pass actual API tests. Applying filters resets the page; paging keeps them; changing call/visit type resets the incompatible outcome; invalid dates keep input and send no request.
- [x] One shared `--page-gutter` matches the prior servant-list spacing: `clamp(12px, 2vw, 24px)`. Authenticated layouts own the gutter; nested page/container padding, narrow outer wrappers and first-heading margin collapse are removed. Dashboard, reports, personal history and other internal pages now use the same outer frame. Card/dialog internal padding and standalone login/onboarding composition remain independent.
- [x] Fix the request-add form's shrinkable grid tracks and wrapping actions; it fits 320px with 200% text size without page-wide overflow.
- [x] Eight SQL Server cases pass (five role/scoping/filter cases plus three import/archive regressions), with zero failures/skips. Both production builds pass: Admin 363.51 kB and Servant 358.06 kB, below 500 kB.
- [x] Initial final browser run: 102 cases pass across Chromium/Firefox/WebKit (36 new spacing/activity cases at eight widths plus landscape/zoom, six print cases and sixty visual-state cases). Applicable existing state baselines refreshed.
- [x] Final route/report capture gate: all eighteen route/report cases pass on three browsers (every reachable route in both apps at 320/1920px, and report captures at 390/1440px). Eighteen church-report baselines refreshed; final personal-history portrait inspected. One repeated Firefox personal-history case had a locator timeout; both Firefox personal-history cases pass in isolation without source changes. Combined distinct passing browser coverage is 120 cases. Results are retained in `activitySpacingRevision` in `docs/verification-results.json`.
- [ ] Publish the API and both clients before expecting this revision on the live site. Runtime/ImageSharp versions, startup migrations and Pages workflows remain unchanged.

## Unified filtered pages (2026-10-12)

| Finding | Change | Verification |
|---|---|---|
| Addition, assignment, removal and personal history appear as separate destinations | One requests route and sidebar entry; all-kind/type/status/personal filters; URL-backed state and retained legacy redirects | SQL all-kind count/order/page and five-role scope; browser mixed-kind actions, filter/paging/refresh and notification links |
| Mixed review actions can target the selected filter rather than the row | Row kind controls approvals, rejection, withdrawal and stale-detail reload | Mixed feed approval, requester withdrawal and stale rejection/browser regression; existing SQL approval transaction/retry tests |
| Area/year/birthday group reports duplicate their implementation | One distribution report and selector; old routes redirect; unused source/template pairs removed | Each grouping and missing-group drilldown, refresh/back, responsive and accessibility verification |
| Weekly servant activity duplicates the period report | One report with custom-period and server-week options; weekly link redirects | Week value mapping, period filters retained on refresh, report rows and links |
| Arabic action reasons cannot be sent as raw HTTP headers | Shared transport encoder used by both API-scoped interceptors and marked server decoder | Actual SQL API audit stores the original Arabic reason; browser withdrawal/rejection plus session regressions |

- [x] Eight SQL Server tests pass, with zero failures/skips. Existing single-kind contracts and approval retries remain valid.
- [x] Both production builds pass: Admin 366.32 kB and Servant 360.54 kB, below the 500 kB limit.
- [x] Final browser gate: 105 distinct cases pass across Chromium/Firefox/WebKit, including unified workflows, eight widths plus landscape/reflow, session regressions, existing route coverage and visual states. The broad run passed 102 cases and exposed three missing-birthday drilldown failures. After preserving `birthMonth=0` through the list and service, all twelve affected drilldown/responsive cases pass (three previously failed plus nine repeats), including a known May group. Final portrait captures inspected and affected maintained baselines refreshed. Evidence is recorded separately in `unifiedPagesRevision` in `docs/verification-results.json`.
- [ ] Publish the coordinated API and both clients. Runtime/ImageSharp, startup migrations and the Pages workflow remain unchanged.


## Scrollable resource choices and compact filter actions (2026-10-13)

| Finding | Change | Verification |
|---|---|---|
| Dropdowns require next/previous buttons | Shared searchable combobox appends lookup pages when its list scrolls; bounded list and existing table pagers | Both apps select and save later-page IDs; no dropdown page buttons |
| Repeated scrolling or old search responses can replace options | One pending append, immediate cancellation, debounced query/version reset, stable ID deduplication and non-advancing-page termination | Pending-page repeated scrolls, out-of-order search and duplicate-row fixtures |
| A failed later page removes usable choices | Preserve prior options; visible retry requests the same failed page and restores input focus | Additional-page failure/retry and failed/empty first pages |
| Current owners disappear during paging/search | ID-bound selected-name cache and explicit current-option fallback; normalize the unassignment sentinel at the parent boundary | Existing off-page owner, search with no matches, clearing and actual unassign request |
| Keyboard focus enters the scroll container in Firefox | Explicit negative list tabindex; combobox active descendant, arrows/Home/End/Enter, Escape and outside/focus-out handling; modal tab boundaries exclude negative-tabindex controls | All three browsers; repeated participant choices, dialog containment/restoration and existing Tab-cycle regression |
| Filter labels are long and action buttons split or differ in height | Short apply/clear labels and one shared full-row, non-wrapping action group; consistent filter-button typography | Eight widths, two landscape sizes, 200% text, same-row/same-height/44px assertions; requests, history, reports, legacy filters and the Servant student list |
| Dropdown/theme changes disturb responsive reports | Shared theme tokens, wrapping names, bounded scrolling and top-aligned open grids | Open-dialog Axe scans, no page-wide overflow, retained report visual states |

- [x] Both production builds pass: Admin 368.11 kB, Servant 362.32 kB; below 500 kB.
- [x] Core verification: 105 distinct cases pass across Chromium/Firefox/WebKit. The initial combined run passed 80 cases, then stopped on one WebKit geometry-measurement failure with 24 cases unexecuted. After reading both rectangles atomically once fonts are ready, all 35 WebKit cases pass (ten repeats, one corrected measurement and 24 remaining cases).
- [x] A further 69 regression cases pass: 15 filter/workflow/export/print cases and 54 report visual cases. Sixty changed maintained report baselines updated; portrait button groups and dropdown captures inspected. Combined distinct passing coverage is 174 cases, recorded in `dropdownAndFilterActionsRevision` in `docs/verification-results.json`.
- [ ] Publish both frontend clients to make the revision visible on the live site. No API/runtime/ImageSharp/startup-migration/Pages workflow changes are part of this revision.
- [ ] Remove only this revision's temporary development probe outputs. Automatic approval review rejected the guarded deletion command with the stated reason `blocked by policy`; the ignored outputs remain under `5.FrontEnd/test-results`. Reusable tests and maintained baselines are needed files.


## Global service-year filter — 2026-10-13

The header in both applications now offers **عام الخدمة** and **الكل**. A year starts on the strict first Friday after September 12 and ends on the strict last Friday before September 12 of the following year. 2026–2027 is September 18, 2026 through September 10, 2027; Cairo's next midnight is the exclusive event boundary.

The user explicitly selected complete current student/servant lists. People are not excluded because they were created in an earlier year. Meetings/attendance use meeting dates, calls/visits use event dates, tasks use due dates, requests use submission dates, and activity/audit history uses timestamps. Current roster statistics remain current. Authentication, notification settings, writes and background processing retain their complete operational scope.

| Finding | Change | Verification |
|---|---|---|
| No common service-year selection | One shared accessible header selector and app/account-specific persisted state; same-app tab synchronization | Both apps, route transitions, reload, open draft retention and 8 widths across 3 engines |
| Incorrect Friday/day boundaries would alter attendance and activity | Strict shared calendar rules, inclusive Cairo final date and exclusive next-day UTC instant | 2024/2025/2026 examples, September 12 Friday cases, timezone-independent client utility checks and real SQL boundary records |
| Existing historical reads bypass ordinary EF filters | Scoped normal event queries plus explicit temporal predicates after archival-filter bypass; raw attendance SQL scoped too | Events, visits/participant credit, requests, tasks, raw attendance and group aggregates; archived histories and current rosters |
| Different scope in summaries/export/print or cached concurrent reports | API-selected scope in all report reads, export queries, print queries, metadata and report coalescing keys | SQL reports/XLSX plus client export/print parameter parity |
| “All” could silently retain a default month limit | Explicit `serviceYear=all` contract removes implicit month defaults; omitted parameter keeps old API defaults | All-history activity and attendance assertions; full report filter workflow checks |
| Changing years could erase local period/drafts or display a stale request | Preserve explicit local dates and business forms; cancel superseded temporal reads and reset page state | Local report date filters survive year changes/reload; two-tab open call drafts remain intact |

- [x] Both production builds pass: Admin 370.59 kB, Servant 364.90 kB; below 500 kB.
- [x] Five utility checks and 30 distinct SQL integration tests pass. Five service-year cases additionally rerun after expanded visit/all/disjoint-period assertions.
- [x] 144 distinct browser cases pass: 42 global-year cases, 33 existing filter-layout cases, and 69 report/workflow regressions (15 functional, 54 visual). The final 42-case rerun verifies local-period persistence and final header styling. Sixty-six retained report snapshots exercised, with updated baselines where needed.
- [x] Cairo inclusive boundaries, role/actor scope, current roster preservation, archival history and command independence verified. No schema migration, runtime/ImageSharp upgrade, startup-migration removal or workflow change.
- [ ] Publish the API and both frontend applications to make the feature available on the live site. The Pages workflow remains frontend-only.


## Bulk student academic-year change — 2026-10-14

The Admin student toolbar now includes **تغيير السنة الدراسية للمحددين** alongside bulk servant assignment. It uses the same cross-page selection, and accepts either an existing academic-year label or a new trimmed value (1–100 characters). This edits the student's academic-year field; the separate global service-year attendance/activity filter keeps its existing purpose.

| Finding | Change | Verification |
|---|---|---|
| Academic-year changes required individual edits | One privileged batch endpoint and shared-theme dialog | Admin/Manager/Priest API tests; cross-page two-student browser save |
| Bulk updates could overwrite stale or unrelated fields | Capture selected row versions; update only academic year/timestamp; return current data for explicit conflict review | Stale-row partial result, reviewed retry, legacy invalid phone/unrelated fields preserved |
| Failures could lose selections or produce duplicate changes | Retain failed IDs and input, prevent duplicate submits, treat already-saved target value as a safe replay | Transport failure/retry, partial result, archived/missing rows, repeated request without duplicate edit history |
| Changes need a reliable history | Save successful rows, per-student edit history and structured field audit in one owned transaction | Actor/name, reason/correlation, old/new academic-year values and enclosing rollback assertions |
| Another bulk action increases mobile toolbar width | Wrap shared-theme action buttons; use the common accessible dialog with separate action spacing | 8 widths, 2 landscape sizes, 200% text reflow, 3 engines, Axe scans, Escape and result focus/visibility |

- [x] Backend and both production frontend builds pass. Initial bundles: Admin 370.59 kB, Servant 364.90 kB; below 500 kB.
- [x] Twelve SQL integration cases pass: 6 feature cases plus 6 existing workflow/import/archive regressions.
- [x] Fifty-four browser cases pass: 12 new behavior, 33 responsive/accessibility, 9 existing selection/phone regressions. Six completed-operation focus/visibility cases rerun after the final focus adjustment.
- [x] Desktop/portrait dialog captures inspected. No schema/runtime/ImageSharp/startup-migration/Pages workflow change.
- [ ] Publish the API and Admin frontend to enable the action on the live site. The deployment workflow remains frontend-only.


## Fixed academic-year choices and data normalization — 2026-10-14

This revision supersedes the free-input academic-year control described above. All write controls share exactly **سنة اولى، سنة ثانية، سنة ثالثة، سنة رابعة، سنة خامسة، سنة سادسة، سنة سابعة، حديث التخرج**. Optional missing values remain supported. Existing read filters retain legacy choices until the database is normalized.

| Finding | Change | Verification |
|---|---|---|
| Free-text years create inconsistent groups | Shared fixed options in Admin add/edit/bulk, both student-detail pages and Servant addition requests | Exact eight options checked at every write entry point in Chromium/Firefox/WebKit |
| API/import callers could reintroduce spelling variants | Shared server canonicalization and validation on create/import/request/edit/bulk | Known aliases stored canonically; unsupported new values rejected |
| Existing ambiguous values could block unrelated edits or be guessed incorrectly | Display current unknown grade for review; omit unchanged grade on save; preserve secondary-school and generic-graduate values | Both detail UIs and API unrelated-edit tests |
| Existing students have inconsistent year spelling | Standalone preview-first SQL script for all students including archived, with explicit apply switch and unknown-value report | Real SQL preview leaves data unchanged; apply updates known values, preserves unrelated data, and writes audit/edit logs |
| Repeated or concurrent normalization might duplicate history or overwrite edits | Immutable captured binary row-version, transaction, before/after history and idempotent target comparison | SQL apply twice produces no extra updates/history; preview snapshots use captured version bytes |
| Dropdown change could affect existing batch behavior/layout | Retain shared theme, busy states, failed selections and conflict review | 45 bulk browser cases across three engines, eight widths, landscape/reflow and accessibility |

- [x] Backend and both production frontend builds pass: Admin 370.59 kB, Servant 364.90 kB. Runtime/ImageSharp, automatic startup migrations and frontend-only Pages workflow unchanged.
- [x] Seven utility cases, 27 SQL Server cases and 57 browser cases pass (91 distinct cases). Final mobile/desktop captures inspected. Evidence in `academicYearChoicesRevision` in `docs/verification-results.json`.
- [ ] Publish the API and both frontend clients.
- [ ] Review the preview from `docs/sql/normalize-student-academic-years.sql`, then set `@Apply=1` to save against the intended database. This script has only been applied to an isolated test database.


## Reviewed graduate values and unknown academic year — 2026-10-14

The user clarified that every graduate is represented as **حديث التخرج**, and **غير معروف** must remain an explicit selectable value. This supersedes the earlier conservative graduate mapping and extends the shared list to nine choices.

| Reviewed input | Canonical value |
|---|---|
| خريجين، خريجة، خريج، اتخرجت 2025، حديثي التخرج | حديث التخرج |
| غير معروف | غير معروف (no data/history change when already exact) |
| سنة رابعه ( هيتخرج ٢٠٢٧ | سنة رابعة |

- [x] Apply identical rules in the shared client helper, server validation/canonicalization and preview-first SQL script. All write dropdowns include غير معروف.
- [x] Preserve other unresolved stages for explicit review; remove only supported future-graduation parenthetical notes.
- [x] Eight utility cases and 35 SQL Server cases pass, covering the supplied values, API/import/request normalization, unchanged unknown values, audited/idempotent SQL, and existing bulk/import/archive behavior. Both production frontend builds pass within the 500 kB initial budget.
- [x] Final browser gate: 57 cases pass across Chromium/Firefox/WebKit, including all nine write choices, saving غير معروف, eight widths, landscape/reflow and accessibility. Corrected an intermittent Firefox result-focus race by waiting for Angular rendering before focusing after dialog closure. Final portrait capture inspected. Total passing coverage: 100 distinct cases.
- [ ] Publish API/both clients and separately review/apply the supplied SQL against the intended database. Runtime/ImageSharp/startup migrations/Pages workflow unchanged.


## Approved areas and audited area normalization — 2026-10-14

Use the user-approved 40 unique labels across all write controls. **المرج الجديده** maps to **المرج الجديدة** rather than producing duplicate options. Retain the supplied canonical spelling of every other approved area, including **اخرى**. Existing read filters retain unresolved legacy values until normalization/review.

| Finding | Change | Verification |
|---|---|---|
| Free-text area fields create spelling variants | Shared fixed choices in Admin add/edit, both details pages and Servant addition request | Exact 40 options checked in all write entry points across 3 engines |
| API/import/request callers could reintroduce variants | Same approved-label normalization on server/client/SQL, and validation of new values | Every approved label, common Arabic spelling variants, create/import/addition, and unknown rejection |
| Distinct neighborhoods might be merged or unknown data erased | Exact normalized-name matching; preserve distinct names and leave unknown values reviewable | شبرا البلد versus شبرا مصر, unknown stage retained on unrelated edit, explicit اخرى selection/save |
| Area changes need the same group workflow as academic years | Admin cross-page area action shares modal, selections and backend transaction/concurrency helper with the year endpoint | Privileged/assigned-user roles, missing/archived/stale outcomes, replay, review/retry, saving guards, failed-ID retention |
| Area changes could overwrite unrelated history or fail partially | Update only Area/timestamp, with edit history and structured field audit in one operation transaction | Before/after values, actor/reason, unrelated academic year/phone/address/notes preserved and rollback |
| Old database values differ from approved choices | Preview-first `docs/sql/normalize-student-areas.sql`, including archived students; unresolved-value report | All 40 canonical values plus aliases tested on real SQL Server; preview/read-only, audited apply and idempotent repeat |
| Extra field action can widen the mobile toolbar | Wrap existing themed toolbar and reuse the common accessible dialog | Eight widths, landscape/reflow, no page overflow, reachable controls, Axe, focus restoration; portrait/desktop captures inspected |
| Shared test data affected full-suite assertions | Give Identity race cases their own database, and archive preview its own test phone | Full 118-case SQL Server integration gate passes without product behavior changes |

- [x] Backend and both production frontend builds pass: Admin 370.59 kB, Servant 364.90 kB, below 500 kB.
- [x] Ten utilities, 118 SQL integration cases and 123 browser cases pass (251 distinct). Browser coverage includes 57 new area cases, 57 academic-year regressions and 9 selection/phone regressions. Evidence in `studentAreaRevision` in `docs/verification-results.json`.
- [x] No schema/runtime/ImageSharp/startup-migration/Pages workflow change.
- [ ] Publish API and both frontend clients.
- [ ] Review the SQL preview (@Apply=0), then set @Apply=1 to save in the intended database. The script has only been applied to isolated test data.


## Reviewed neighborhood aliases — 2026-10-14

The following explicit mappings extend area normalization while retaining the same 40 dropdown choices. No fuzzy, substring or blanket city-name merge is applied.

| Reviewed old value | Approved destination |
|---|---|
| شبرا المحطة | شبرا البلد |
| شبرا الخيمة | شبرا البلد |
| شبرا الخيمه | شبرا البلد |
| ارض نوبار شبرا الخيمه | ارض نوبار |
| المساكن | الوحدة |
| الملف شبرا الخيمه | الملف |
| الوحدة عند المصانع | الوحدة |
| خلف الكنيسة | شبرا البلد |
| شارع زكريا | شبرا البلد |
| منية السيرج | اخرى |

- [x] Identical mappings in client/server helpers and `docs/sql/normalize-student-areas.sql`. The two شبرا الخيمة spellings use a single SQL alias key.
- [x] All supplied mappings verified through canonicalization and real SQL preview/apply/idempotent audit; API create/edit/addition/import and bulk aliases covered. Other unresolved values remain reviewable.
- [x] Backend and both production frontend builds pass. Eleven utility, 34 distinct SQL and 12 browser cases pass (57 distinct); six bulk cases additionally rerun with المساكن → الوحدة. Existing responsive layouts retained; dropdown count remains 40.
- [ ] Publish API/both clients and separately review/apply the updated SQL script against the intended database. Runtime/ImageSharp, automatic startup migrations and Pages workflow unchanged.


## Unlisted area fallback to اخرى — 2026-10-14

Per explicit user instruction, this supersedes preserving/rejecting unlisted nonempty area names. Approved names and reviewed aliases retain priority; all remaining nonempty names become **اخرى**. Empty/null area stays **غير مسجلة**. Omitted PATCH fields retain their existing meaning.

- [x] Apply identical client/server fallback for create/edit/import/addition/bulk, including explicit re-submission of an unchanged old unlisted name. Keep field length validation and preserve unrelated fields.
- [x] Edit dropdowns select اخرى for unlisted old values; remove obsolete Admin unknown-area review choice. Academic-year handling and the 40 approved area choices unchanged.
- [x] Update preview-first SQL to include unlisted areas, including archived students, with transaction, row-version protection, audit/edit history and idempotent repeat. Empty areas remain missing.
- [x] Backend/both frontend builds pass; 11 utilities, 70 SQL integration cases and 24 browser cases pass (105 distinct). One SQL case additionally rerun with an archived unlisted area. Evidence in `studentAreaFallbackRevision` in `docs/verification-results.json`.
- [ ] Publish API/both clients and separately review/apply `docs/sql/normalize-student-areas.sql` to the intended database. No production data modified; runtime/ImageSharp/startup migrations/Pages workflow unchanged.


## Explicit 32 approved areas and catalogue alignment — 2026-10-14

This revision replaces the earlier 40-name list with the user's explicitly approved 32 choices, including **اخرى**. The following old names (and spelling variants) now become **اخرى**: الخانكة، الزيتون، العبور، المرج، المرج الجديدة، المعادى الجديدة، النزهه الجديده، حدائق الاهرام. Previously approved neighborhood aliases retain their destinations. Blank areas remain optional.

| Finding | Change | Verification |
|---|---|---|
| Earlier database names were treated as approved choices | Replace server/client catalogue and SQL allowed names with the exact supplied 32-name list | Independent exact client-list assertion; all server choices and SQL outcomes tested |
| Eight unwanted areas remained accepted | Remove them from canonical alias rows; retain fallback to اخرى | Explicit eight-name tests; SQL apply includes an archived العبور student and preserves other fields/history |
| Filter dropdowns rebuilt choices from stored records | Use fixed shared catalogue in both student lists and both report pages; compatible API lookup returns same catalogue | Four filter surfaces tested against fixtures containing unapproved database areas; API equality checked |
| Users could confuse preview with persistence | Keep Arabic mode/database/conversion-count output and explicit @Apply instructions | SQL preview changes nothing, apply updates areas/history atomically, repeat produces no duplicate changes |

- [x] Backend and both frontend production builds pass; Admin 370.59 kB, Servant 364.90 kB, below 500 kB.
- [x] Twelve utilities, full 141-case SQL integration suite and 135 browser cases pass (288 distinct). Three engines, eight widths, landscape/reflow, accessibility and existing year/selection workflows included. Final mobile area dialog inspected. Evidence in `approvedAreas32Revision` in `docs/verification-results.json`.
- [x] Runtime/ImageSharp, automatic startup migrations, schema and Pages deployment workflow unchanged.
- [ ] Review the updated `docs/sql/normalize-student-areas.sql`, set `DECLARE @Apply bit = 1`, and run it completely against the intended database to update real data.
- [ ] Publish API and both frontend applications; the live site continues to use its deployed version until publication.


## Bulk servant choice includes غير مسند — 2026-10-14

- [x] Add **غير مسند** to the shared picker in the Admin selected-student assignment dialog; explicit selection enables **تأكيد إلغاء الإسناد** and posts `servantId:null`. Initial no-choice remains disabled.
- [x] Keep failed IDs and chosen target for retry; handle missing per-row results, partial failures and transport failures. Lock dependent controls and duplicate saving.
- [x] Show assignment/unassignment outcome and dialog errors with accurate wording; keep CSV import feedback separate; focus successful result feedback.
- [x] Four SQL cases and fifteen browser cases pass (nineteen distinct). The batch API test verifies unassignment without archival, open tasks moved to unassigned ownership, and preserved historical call actor. Three-engine browser checks include cross-page/null payload, failure retry, saving locks and existing assignment/attendance/phone behavior; 390px capture inspected.
- [x] Admin production build passes at 370.59 kB. No backend production, schema/runtime/ImageSharp/startup-migration/Pages workflow change; separate service-year selector edit retained.
- [ ] Publish the updated Admin frontend to show the option on the live application.


## Dedicated servant workspace — 2026-10-14

| Finding | Change | Verification |
|---|---|---|
| Servant app inherited unrelated management reports | Explicit seven-report catalog with personal names and descriptions; reject management report deep links and exports | Servant/Manager browser catalog tests and scoped API report/export rejection |
| Manager account could use the servant client as a global management workspace | Personal presentation context plus API-only restrictive workspace header and controller metadata; management-only role endpoints reject this mode | Same Manager token verified with personal scope and original admin scope; own requests/tasks, unrelated details/photos and role-restricted operations tested |
| Contact reports credited other servants' work | Use scoped actor queries for calls and recorder/participant visits; distinct visit records and matching dashboard definition | Mixed actors, shared visit, another-actor-only contacted student, full print, CSV contents and XLSX row counts tested |
| Student list initially mixed own group with discovery | Default Assigned segment with SQL filtering; explicit minimal unassigned view for requests | Default/reset choice, pagination population and discovery privacy verified; legacy omitted segment preserved |
| General labels and filters confused personal use | Own group/tasks/activity/requests labels and dashboard links; no processor/actor selectors or hidden name filter for personal attendance | Three-engine navigation, copied URLs, filter/print/export and request/session regressions |

- [x] Both production frontend builds pass: Admin 370.59 kB and Servant 364.82 kB, below 500 kB.
- [x] Full 142-case SQL integration suite passed; final expanded personal-workspace API case also passed. 162 distinct browser cases passed across Chromium, Firefox and WebKit, including isolated retries of two Firefox load/networkidle timeouts. Evidence and run notes in `servantWorkspaceRevision` in `docs/verification-results.json`.
- [x] Own pages and seven report types fit all eight required widths, landscape and enlarged text checks. Personal report visual states updated; 390px populated capture inspected. Shared theme and existing student-attendance duty retained.
- [x] No schema, ImageSharp/.NET, automatic startup migration or Pages workflow change.
- [ ] Publish the API first, then updated frontend applications. Frontend-only publication does not complete personal API scoping; no database migration is needed for this revision.


## Illustrated user manuals for both frontends — 2026-10-15

| Finding | Change | Verification |
|---|---|---|
| Users lacked a complete, accessible guide | Shared themed help reader, question-mark header links and separate Admin/Servant content | Header access, app scope, all active page coverage and deep links checked |
| Pages and operations needed practical explanation | 47 Admin chapters and 28 Servant chapters with purpose, numbered steps, caveats and contextual links | Each chapter traversed; attendance links withheld from ordinary servants |
| Report numbers could be misunderstood | Explain all 17 Admin and 7 Servant reports, including source scope, formulas, distinct people versus attempts, historical ownership, confirmed rosters, empty denominators and export scope | Calculation coverage assertions and review against report-provider implementations |
| Text alone could not demonstrate the workflows | 137 native captures: 87 Admin and 50 Servant; fictional records and intercepted APIs, operation dialogs and report views | Complete capture manifests, image dimensions, all referenced assets decoded in the reader and print checks; loaded desktop/mobile guide captures inspected |
| Users need lookup, sharing and printed instructions | Arabic search, grouped contents, bookmarked chapters, full-size images, whole-guide/current-part print with contents and browser Save as PDF | Search/bookmarks/keyboard and full/part print checks on three engines; incomplete image/chapter downloads show readable recovery |
| A large manual could slow ordinary app use | Lazy route/content and per-app lazy image assets; retain shared theme and responsive layout | Admin 370.77 kB and Servant 365.00 kB initial builds; help images absent from prefetch app assets |

- [x] Both production builds pass without warnings and remain below 500 kB.
- [x] Ninety distinct browser cases pass across Chromium, Firefox and WebKit, including the eight required widths, landscape, 200% text, accessibility, failed downloads, image decoding and deployed subpaths. Two native capture generation jobs pass. Details in `userManualRevision` in `docs/verification-results.json`.
- [x] No backend, schema, runtime/ImageSharp, automatic startup migration or frontend-only deployment workflow change.
- [ ] Publish the updated frontend applications to expose the header icon and manual on the live site.
