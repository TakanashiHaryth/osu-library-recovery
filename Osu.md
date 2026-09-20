#### **PRODUCT REQUIREMENTS DOCUMENT** 

# **Beatmap Library Recovery** 

Open source recovery tool for osu!lazer beatmap libraries 

|**Field**|**Value**|
|---|---|
|Document version|0.1|
|Status|Draft for community review|
|Date|20 September 2026|
|Primary platform|Windows desktop|
|Future platforms|Linux and macOS|
|Recommended language|C# on .NET 10 LTS|
|Recommended UI framework|Avalonia UI 12.x|
|Proposed licence|MIT Licence|



**Project position** This is an independent community project. It must not present itself as an official osu! or ppy Pty Ltd product. 

Page 1 

Beatmap Library Recovery PRD  |  Version 0.1 

## **1 Product Summary** 

Beatmap Library Recovery helps osu!lazer players rebuild a playable beatmap library when the local map database is missing, damaged, moved incorrectly, or no longer recognises stored content. The application discovers beatmaps associated with the player's server-side play history and optional local recovery artefacts, lets the player choose which beatmap sets to restore, and then produces a portable recovery bundle or imports the selected sets through supported osu!lazer mechanisms. 

The product reconstructs a library rather than recreating the original database byte for byte. It cannot recover offline-only activity after every local record has been lost, and it must not write directly to osu!lazer's live client.realm database or hashed files store. 

## **2 Product Decision** 

**Recommended implementation** Build the desktop application in C# on .NET 10 LTS with Avalonia UI 12.x. Use a modular core library, SQLite for the tool's own state, the official osu! API for discovery, and supported .osz or osu!lazer import paths for restoration. 

**Reason for the choice** C# fits the osu!lazer ecosystem, provides strong desktop and networking libraries, supports asynchronous download queues and archive processing without extra runtimes, and allows a Windows-first release to expand to Linux and macOS through Avalonia. .NET 10 is the active LTS release and is supported until November 2028. 

**Important dependency** The team must validate an authorised route for downloading full and no-video .osz packages. If third-party access to the official download route is restricted, the first release will provide autoimport through the installed osu!lazer client and a manifest-only export until an approved ZIP workflow is available. 

## **3 Problem Statement** 

osu!lazer stores imported files under content hashes and maps them through client.realm. A player may still have raw data in the files directory while the database can no longer associate it with beatmaps. In more severe cases, both the database and the local files are gone. Players then have to inspect profile history manually, open many beatmap pages, choose download variants one at a time, and repeat imports. This process is slow, error prone, and difficult to resume. 

A recovery tool can reduce this work by converting available history into a deduplicated checklist of beatmap sets and a repeatable restoration job. It must clearly distinguish server-confirmed history, local evidence, unavailable maps, and assumptions. 

Page 2 

Beatmap Library Recovery PRD  |  Version 0.1 

## **4 Goals** 

- Discover beatmap sets associated with a player's available online play history across supported rulesets. 

- Optionally inspect user-selected copies of local recovery artefacts without modifying the live osu!lazer store. 

- Present a deduplicated, searchable selection list with availability and recovery confidence. 

- Let the user choose full packages or packages without background video when the download source supports that option. 

- Create a resumable recovery job that produces a ZIP of .osz files, a download folder, an auto-import operation, or a manifest fallback. 

- Keep authentication, stored tokens, logs, and optional diagnostics private by default. 

- Publish the application, documentation, issue templates, and build process as an open source project. 

## **5 Non Goals** 

- Reconstructing the original client.realm file exactly. 

- Restoring local scores, replays, collections, settings, or multiplayer room history in the minimum viable product. 

- Editing or injecting records into osu!lazer's live database. 

- Operating a public beatmap mirror or redistributing beatmaps in project releases. 

- Recovering maps that are no longer legally or technically available from an approved source. 

- Collecting the user's osu! password, browser cookies, or unrelated account information. 

- Guaranteeing discovery of offline-only plays after all local evidence has been lost. 

## **6 Target Users** 

|**User**|**Situation**|**Primary need**|
|---|---|---|
|Player after reinstall|The computer or osu!lazer data<br>directory was reset|Rebuild a useful library from server<br>history|
|Player with database damage|client.realm no longer exposes<br>previously imported beatmaps|Recover available maps without writing<br>into the damaged database|
|Player moving storage|A relocation left the new installation<br>empty|Compare discovered history with the<br>current library and restore missing sets|
|Advanced user|Old Realm files, logs, exports, or<br>manifests still exist|Merge multiple evidence sources and<br>export a detailed report|



## **7 Core User Journey** 

1.  The user opens the tool and chooses Online Recovery or Local Assisted Recovery. 

2.  The tool identifies the osu! account through username lookup or an approved OAuth flow. 

3.  The tool retrieves paginated play history, recent plays, and optional local evidence, then deduplicates results by beatmap set ID. 

4.  The tool displays the result table with filters, availability, play count, evidence source, and estimated download size when available. 

Page 3 

Beatmap Library Recovery PRD  |  Version 0.1 

5.  The user ticks individual sets or applies bulk selection rules, then chooses Full, No Video, or a per-map override. 

6.  The user chooses ZIP Bundle, Download Folder, Import to osu!lazer, or Manifest Only. 

7.  The tool downloads with bounded concurrency, validates each response, records failures, and resumes interrupted work. 

8.  For auto-import, the tool hands valid .osz files or supported direct links to osu!lazer and reports which sets were accepted. 

9.  The user receives a final recovery report with completed, skipped, unavailable, and failed items. 

## **8 Recovery Evidence Model** 

The application assigns each discovered set an evidence level. Evidence affects how the interface labels the result but does not silently exclude lower-confidence items. 

|**Level**|**Source**|**Interpretation**|
|---|---|---|
|Confirmed online|Server-side most played or recent<br>score record|The account played or submitted<br>activity for at least one difficulty in the<br>set|
|Confirmed local|Readable metadata from a user-<br>selected database copy or exported<br>artefact|The local installation previously knew<br>the set|
|Probable local|Log, replay filename, collection<br>reference, or manifest|The set is referenced but full map<br>metadata may require lookup|
|Unresolved|A reference cannot be matched to an<br>available beatmap set|The item remains in the report and is<br>not downloaded automatically|



## **9 Functional Requirements** 

|**ID**|**Capability**|**Requirement**|**Priority**|
|---|---|---|---|
|ACC 01|Account lookup|Accept an osu! username or<br>numeric user ID and resolve it<br>to a confirmed account before<br>discovery.|Must|
|ACC 02|OAuth|Use browser-based OAuth<br>when user-authorised endpoints<br>are required. Never request the<br>osu! password inside the<br>application.|Must|
|DSC 01|Most played history|Retrieve paginated most played<br>data for each selected ruleset<br>and preserve play counts.|Must|
|DSC 02|Recent history|Merge recent scores and<br>include failed plays when the<br>API permits it.|Must|
|DSC 03|Local evidence|Scan only paths selected by the<br>user and read recovery<br>artefacts from a copy or read-<br>only handle.|Should|



Page 4 

Beatmap Library Recovery PRD  |  Version 0.1 

|**ID**|**Capability**|**Requirement**|**Priority**|
|---|---|---|---|
|DSC 04|Deduplication|Merge duplicate difficulties and<br>evidence records into one<br>beatmap set entry.|Must|
|LIB 01|Current library comparison|Optionally compare results with<br>the current installation and mark<br>sets that appear installed.|Should|
|SEL 01|Selection|Support individual checkboxes,<br>select all visible, clear all, invert<br>selection, and rule-based bulk<br>selection.|Must|
|SEL 02|Filters|Filter by ruleset, status, artist,<br>title, mapper, play count,<br>availability, evidence source,<br>and recovery state.|Must|
|VAR 01|Download variant|Support Full and No Video<br>choices globally and per<br>beatmap set when the source<br>supports both.|Must|
|OUT 01|ZIP bundle|Produce one outer ZIP<br>containing separate .osz<br>packages plus manifest.json,<br>manifest.csv, checksums, and a<br>report.|Must|
|OUT 02|Download folder|Save validated .osz packages<br>to a user-selected folder without<br>importing them.|Must|
|OUT 03|Auto import|Use a supported osu!lazer<br>import mechanism and never<br>write directly to client.realm or<br>files.|Must|
|OUT 04|Manifest fallback|Export the complete selection<br>and official beatmap URLs<br>when automated package<br>retrieval is unavailable.|Must|
|JOB 01|Resume|Persist job state and resume<br>incomplete downloads after<br>restart without redownloading<br>verified packages.|Must|
|JOB 02|Rate control|Apply bounded concurrency,<br>server-aware throttling,<br>exponential backoff, and retry<br>limits.|Must|
|JOB 03|Integrity|Reject HTML error pages,<br>empty responses, mismatched<br>content, and invalid archives<br>before packaging or import.|Must|
|RPT 01|Recovery report|Record completion, skip reason,<br>failure reason, source, selected<br>variant, and timestamps for<br>every set.|Must|
|PRV 01|Private by default|Disable analytics and diagnostic<br>uploads by default. Redact<br>tokens and secrets from logs.|Must|



Page 5 

Beatmap Library Recovery PRD  |  Version 0.1 

|**ID**|**Capability**|**Requirement**|**Priority**|
|---|---|---|---|
|UX 01|Accessibility|Support keyboard operation,<br>scalable text, visible focus,<br>screen-reader labels, and non-<br>colour status cues.|Should|



Page 6 

Beatmap Library Recovery PRD  |  Version 0.1 

## **10 Output Specifications** 

### **10 1 ZIP Bundle** 

The ZIP bundle is a portable recovery package. It contains one unchanged .osz archive per beatmap set rather than merging multiple sets into one .osz file. The outer archive also records what was requested and what actually completed. 

|**Path**|**Purpose**|
|---|---|
|beatmaps/<set-id> <artist> - <title>.osz|Downloaded beatmap set package|
|manifest.json|Machine-readable recovery selection, evidence, metadata,<br>and result state|
|manifest.csv|Human-readable spreadsheet-compatible summary|
|checksums.sha256|Integrity hashes for every included .osz|
|recovery-report.txt|Plain-language summary of completed, skipped,<br>unavailable, and failed items|



### **10 2 Auto Import** 

Auto import downloads to a temporary staging directory, validates each package, then passes it to an import method supported by osu!lazer. The tool waits for an observable acceptance signal when one is available. Temporary files are retained until the user confirms cleanup or the job has a verified result. 

### **10 3 Manifest Only** 

Manifest Only is the compliance and outage fallback. It records beatmap set IDs, official pages, evidence, requested variants, and availability without downloading copyrighted content. A future run can reopen the manifest and continue from the selection stage. 

## **11 Data Requirements** 

|**Entity**|**Required fields**|
|---|---|
|Account|User ID, username, selected rulesets, discovery timestamp|
|Beatmap set|Set ID, artist, title, mapper, status, has video, official URL|
|Evidence|Source type, source reference, play count when available,<br>confidence, discovered at|
|Selection|Selected state, requested variant, output mode, user<br>override|
|Download|State, attempts, bytes, response type, SHA-256, local<br>staging path, failure code|
|Import|Requested at, method, observed result, verified at, cleanup<br>state|
|Recovery job|Job ID, created at, last updated, totals, settings snapshot,<br>completion state|



Page 7 

Beatmap Library Recovery PRD  |  Version 0.1 

## **12 Success Measures** 

|**Measure**|**MVP target**|
|---|---|
|Safety|Zero direct writes to the live osu!lazer database or hashed<br>files store|
|Discovery|All API pages returned for the selected rulesets are<br>processed and deduplicated by set ID|
|Resumability|A terminated job resumes without redownloading files<br>whose hashes were already verified|
|Integrity|No package is imported or added to a bundle unless archive<br>validation succeeds|
|Transparency|Every selected set ends in Completed, Skipped,<br>Unavailable, Cancelled, or Failed with a reason|
|Privacy|No analytics transmission without explicit opt in and no<br>secret values in application logs|
|Usability|A user can discover, select, and start recovery without<br>editing configuration files|



## **13 Technical Direction** 

### **13 1 Selected Stack** 

|**Layer**|**Selection**|**Reason**|
|---|---|---|
|Language|C#|Strong asynchronous I O, archive<br>support, typed domain modelling, and<br>alignment with the osu!lazer ecosystem|
|Runtime|.NET 10 LTS|Active long-term support through<br>November 2028 and self-contained<br>desktop publishing|
|Desktop UI|Avalonia UI 12.x|One C# and XAML codebase for<br>Windows-first delivery with Linux and<br>macOS expansion|
|Local state|SQLite through Microsoft.Data.Sqlite|Transactional job state, resumability,<br>migration support, and straightforward<br>inspection|
|Networking|HttpClient with typed API clients|Built-in cancellation, streaming, headers,<br>retry integration, and testable handlers|
|Archives|System.IO.Compression|Built-in ZIP creation and inspection<br>without an external process|
|Testing|xUnit and integration fixtures|Unit coverage for discovery and job state<br>plus deterministic archive and HTTP<br>tests|
|Build|dotnet CLI and GitHub Actions|Reproducible community builds, tests,<br>checksums, and signed release workflow<br>when available|



Beatmap Library Recovery PRD  |  Version 0.1 Page 8 

### **13 2 Stack Alternatives** 

|**Option**|**Strength**|**Reason not selected for MVP**|
|---|---|---|
|TypeScript and Tauri|Small shell and web UI flexibility|Requires two main languages and Rust<br>boundary work for a contributor-friendly<br>desktop recovery core|
|Rust and Iced|Memory safety and efficient native<br>binaries|Slower UI iteration and fewer contributors<br>likely to understand both the GUI and<br>recovery domain|
|Electron|Fast web development and broad<br>package ecosystem|Larger runtime footprint and weaker fit for<br>a focused utility that performs long-<br>running local file work|
|Python and Qt|Fast prototyping|Packaging, startup, dependency<br>management, and cross-platform<br>distribution are less predictable for end<br>users|



### **13 3 Proposed Solution Structure** 

|**Project**|**Responsibility**|
|---|---|
|Recovery App|Avalonia views, view models, navigation, accessibility, and user<br>interaction|
|Recovery Core|Domain entities, evidence merging, selection rules, job state<br>machine, and interfaces|
|Recovery OsuApi|OAuth abstraction, account lookup, most played and recent<br>pagination, caching, and rate handling|
|Recovery Local|Read-only artefact discovery, current-install comparison, and<br>safe path validation|
|Recovery Downloads|Queue, streaming, validation, retries, checksums, and staging|
|Recovery Packaging|ZIP, CSV, JSON, checksum, and report writers|
|Recovery Import|Supported osu!lazer import adapters and observable result<br>checks|
|Recovery Tests|Unit, integration, migration, corrupted-input, and end-to-end<br>fixtures|



## **14 Authentication and Distribution** 

The official osu! API requires OAuth client registration. A native open source executable cannot keep a static client secret confidential because users can inspect the binary and source. The project therefore separates development credentials from release authentication. 

- Development and self-hosted builds allow contributors to provide their own OAuth client ID and client secret through local settings. Secrets are excluded from source control and logs. 

Page 9 

Beatmap Library Recovery PRD  |  Version 0.1 

- Official binaries may use a small maintainer-operated token broker only after a security and privacy review. The broker must not receive osu! passwords or local recovery data. 

- If a secure and permitted authentication path cannot support a required endpoint, the application falls back to public metadata, manifest export, official web links, or the installed osu!lazer client. 

- Tokens are stored through an operating-system credential service where supported and can be revoked or deleted from the application. 

## **15 Download and Import Policy** 

- Use official osu! endpoints or client mechanisms by default. 

- Do not ship or silently enable an unofficial mirror. Mirror support, if ever added, must be an explicit plugin with clear source and legal warnings. 

- Do not bypass account, supporter, geographic, availability, or abuse controls. 

- Respect server limits, cache metadata, avoid repeated polling, and back off after throttling or transient failures. 

- Never ask the user to paste an osu! session cookie into the application. 

## **16 Safety and Privacy** 

### **16 1 Filesystem Safety** 

- Default to a tool-owned staging directory and require confirmation before writing elsewhere. 

- Resolve and display destination paths before download or cleanup. 

- Use atomic state updates and avoid destructive cleanup until output or import has been verified. 

- Treat client.realm, client backup Realm files, storage.ini, and the files directory as read-only evidence. 

- Reject output paths that overlap protected system locations or the live osu!lazer database unless the operation is a supported import handoff. 

### **16 2 Data Privacy** 

- Keep account identifiers, history, manifests, and logs on the user's device by default. 

- Collect no telemetry in the MVP. A future opt-in system must document fields, retention, destination, and deletion controls. 

- Redact access tokens, refresh tokens, client secrets, cookies, home-directory names, and unrelated file paths from diagnostics. 

- Require explicit user action before attaching diagnostics to a bug report. 

## **17 Failure Handling** 

|**Failure**|**Required behaviour**|
|---|---|
|API authentication fails|Explain whether credentials, consent, scope, clock, or service<br>availability caused the failure and preserve the pending job|
|Rate limit or server error|Pause affected requests, apply backoff, show the next retry,<br>and allow cancellation|
|Map unavailable|Mark Unavailable, preserve metadata and official URL, and<br>continue the rest of the job|
|Response is not an .osz|Quarantine or delete the invalid temporary response, record the<br>content type and status safely, and do not import|



Page 10 

Beatmap Library Recovery PRD  |  Version 0.1 

|**Failure**|**Required behaviour**|
|---|---|
|Disk space is low|Stop before corruption, show required and available space<br>when measurable, and keep completed files|
|Application closes|Commit job progress before exit and offer Resume on the next<br>launch|
|osu!lazer is unavailable|Keep validated files in staging and offer Download Folder or<br>retry import later|
|Local database is locked or damaged|Do not force access or repair it automatically; ask for a copied<br>file or continue with online recovery|



## **18 Acceptance Criteria** 

|**Area**|**Criterion**|
|---|---|
|Discovery|Given a valid user with paginated most played results, when<br>discovery completes, then each unique beatmap set appears<br>once with merged play count and evidence.|
|Selection|Given a filtered result list, when Select All Visible is used, then<br>only rows matching the active filter are selected.|
|Variant|Given a set with a video and an approved source that supports<br>both variants, when No Video is selected, then the resulting job<br>requests the no-video package and records that choice.|
|Bundle|Given three validated .osz files, when ZIP Bundle completes,<br>then the outer ZIP contains the three packages, JSON and<br>CSV manifests, SHA-256 checksums, and a recovery report.|
|Resume|Given a partially completed job, when the application restarts,<br>then verified files remain complete and only pending or<br>retryable items continue.|
|Safety|Given a selected client.realm path, when Local Assisted<br>Recovery runs, then no write operation occurs against that file<br>or its live data directory.|
|Invalid response|Given a download that returns HTML or a damaged archive,<br>when validation runs, then the file is not packaged or imported<br>and the item ends in Failed with a reason.|
|Fallback|Given that package retrieval is not authorised, when the user<br>selects Manifest Only, then the tool exports every selected set<br>ID and official URL without attempting a package download.|



## **19 Test Strategy** 

- Unit tests cover pagination, evidence merging, deduplication, selection rules, job transitions, filename sanitation, and report generation. 

- HTTP integration tests use recorded or synthetic fixtures for success, throttling, expired tokens, redirects, unavailable sets, HTML error pages, truncated content, and cancellation. 

- Archive tests use valid, empty, oversized, malicious-path, and corrupted ZIP fixtures. 

- Persistence tests verify database migrations, crash recovery, idempotent resume, and cleanup state. 

Page 11 

Beatmap Library Recovery PRD  |  Version 0.1 

- Import adapter tests never use a real live database. End-to-end tests use temporary directories and a mocked client handoff. 

- Release checks run on Windows first, followed by Linux and macOS when those packages enter support. 

## **20 Open Source Project Requirements** 

- Use the MIT Licence unless maintainers later choose a reciprocal licence before the first public release. 

- Include CONTRIBUTING, CODE OF CONDUCT, SECURITY, privacy documentation, architecture notes, and a reproducible build guide. 

- Require automated tests and formatting checks for pull requests. 

- Publish release checksums and a software bill of materials. Add code signing when project resources permit it. 

- Do not include beatmap packages, API secrets, tokens, user histories, production database copies, or copyrighted assets in the repository or test fixtures. 

- Use a clear unofficial-project disclaimer and follow applicable osu! branding and API terms. 

## **21 Delivery Phases** 

|**Phase**|**Deliverable**|**Exit condition**|
|---|---|---|
|Phase 0|Technical spike|Confirmed most played pagination,<br>authentication model, approved package<br>download route, no-video behaviour, and<br>osu!lazer import handoff|
|Phase 1|Discovery preview|Username lookup, online discovery,<br>deduplication, filters, selection, and<br>manifest export work on Windows|
|Phase 2|Recovery MVP|Resumable downloads, validation,<br>download folder, ZIP bundle where<br>authorised, and recovery report pass<br>acceptance tests|
|Phase 3|Auto import|Supported client handoff is reliable and<br>does not modify the live database directly|
|Phase 4|Local assisted recovery|Read-only artefact readers add evidence<br>from copied Realm files, logs, replays,<br>and manifests|
|Phase 5|Cross platform release|Linux and macOS packaging, credential<br>storage, path handling, and import flows<br>pass release tests|



## **22 Risks and Mitigations** 

|**Risk**|**Impact**|**Mitigation**|
|---|---|---|
|Official download access is restricted|ZIP and direct package download cannot<br>ship as designed|Complete Phase 0 before building the<br>downloader; retain manifest and client-<br>assisted import fallbacks|



Page 12 

Beatmap Library Recovery PRD  |  Version 0.1 

|**Risk**|**Impact**|**Mitigation**|
|---|---|---|
|Online history is incomplete|Recovered library misses offline or<br>unsubmitted plays|Label the source clearly and merge<br>optional local evidence without promising<br>total recovery|
|Realm schema changes|Local parser breaks after an osu!lazer<br>update|Keep local parsing optional, version<br>readers, test fixtures by schema, and<br>never make it the only discovery path|
|Large libraries consume storage and<br>bandwidth|Jobs fail or users create duplicate data|Estimate size, support no-video, stage<br>incrementally, resume, deduplicate, and<br>warn before bundle creation|
|Embedded OAuth secrets leak|Credentials can be abused|Do not commit secrets; use user-supplied<br>credentials or an audited broker and<br>rotate compromised credentials|
|Import result cannot be verified reliably|Tool reports success too early|Keep staged packages, report Submitted<br>separately from Verified, and require<br>observable evidence before cleanup|
|Project appears official|Brand confusion and trust risk|Use a neutral name, an unofficial<br>disclaimer, and accurate attribution|



## **23 Open Questions** 

1.  Can a third-party OAuth client use an official beatmap package download route, including the no-video option, without a private or reserved scope? 

2.  Which osu!lazer handoff provides a reliable per-set completion result for auto-import? 

3.  Does the most played endpoint expose the full server-known set history for every ruleset, or is there a practical pagination cap? 

4.  Should the first public release require users to create their own osu! OAuth application, or should maintainers operate a minimal token broker? 

5.  Which local artefacts provide stable, supportable evidence without depending on private Realm internals? 

6.  Should the proposed MIT Licence remain, or does the community prefer MPL-2.0 to keep modifications to core project files open? 

## **24 References** 

- <u>osu! API v2 documentation  OAuth, user scores, most played data, pagination, and API use guidance</u> 

- <u>osu!lazer user file storage  Hashed file storage and client.realm mapping</u> 

- <u>osu!lazer file storage guide  Storage location, relocation, migration, and safe external handling</u> 

- <u>osu! beatmap information  Full and no-video download options</u> 

- <u>Microsoft .NET support policy  .NET 10 LTS support period</u> 

- <u>Avalonia 12.1 release information  Current cross-platform desktop framework direction</u> 

## **25 Definition of Done** 

The MVP is done when a Windows user can identify an account, discover and deduplicate available serverknown beatmap history, select sets and variants, complete a resumable recovery operation through an authorised output path, receive a complete report, and verify that the tool never writes directly to the live 

Beatmap Library Recovery PRD  |  Version 0.1 Page 13 

osu!lazer database or hashed files store. The release must include source code, reproducible build instructions, automated tests, licence and security documents, and no embedded private credentials. 

Page 14 

Beatmap Library Recovery PRD  |  Version 0.1 

