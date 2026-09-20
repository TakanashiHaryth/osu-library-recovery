<div align="center">

<img src="src/Recovery.App/Assets/OsuRecoverLibrary.ico" width="112" alt="Beatmap Library Recovery icon" />

<br />

<img src="https://readme-typing-svg.herokuapp.com?font=Inter&weight=700&size=28&duration=3200&pause=900&color=FF5C9D&center=true&vCenter=true&width=760&lines=Recover+your+osu!lazer+beatmap+library;Discover.+Review.+Validate.+Restore.;Safe+by+design.+Built+for+the+community." alt="Beatmap Library Recovery introduction" />

# Beatmap Library Recovery

### A safe, open-source recovery workspace for osu!lazer beatmap libraries

<p>
  <img src="https://img.shields.io/badge/C%23-.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="C# and .NET 10" />
  <img src="https://img.shields.io/badge/Avalonia_UI-12.x-8B5CF6?style=for-the-badge" alt="Avalonia UI 12" />
  <img src="https://img.shields.io/badge/Platform-Windows-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="Windows" />
</p>

<p>
  <img src="https://img.shields.io/badge/License-MIT-FF5C9D?style=flat-square" alt="MIT License" />
  <img src="https://img.shields.io/badge/Status-Early_Preview-F2C66D?style=flat-square" alt="Early preview status" />
  <img src="https://img.shields.io/badge/Tests-25_Passing-52D6A0?style=flat-square" alt="25 passing tests" />
  <img src="https://img.shields.io/badge/Database_Safety-Read_Only-79A8FF?style=flat-square" alt="Read-only database safety" />
</p>

</div>

---

> [!IMPORTANT]
> Beatmap Library Recovery is an independent community project. It is not affiliated with, endorsed by, or presented as an official product of osu! or ppy Pty Ltd.

> [!CAUTION]
> This repository is an early development preview. There is no official prebuilt release yet, and the current experimental download strategy must be reviewed before public distribution.

## About

Beatmap Library Recovery helps osu!lazer players rebuild a useful beatmap library after a reinstall, damaged database, failed storage move, or missing local records.

The application discovers beatmap sets from available server-side play history, merges optional local evidence, and presents the results in a searchable recovery table. Selected maps can be exported as manifests, downloaded into a folder, packaged into a portable ZIP bundle, or handed to the installed osu!lazer client.

The project reconstructs a playable library. It does **not** attempt to recreate the original `client.realm` database byte for byte.

```text
Discover account history
        |
        v
Merge and deduplicate evidence
        |
        v
Review thumbnails, metadata and availability
        |
        v
Select maps and recovery output
        |
        v
Validate packages and generate a report
```

## Current Features

<table>
<tr>
<td width="50%" valign="top">

### Library discovery

- Resolve an osu! username or numeric user ID
- Paginate through most-played history
- Merge recent scores when OAuth access is available
- Deduplicate difficulties by beatmap set ID
- Preserve play counts and evidence sources

</td>
<td width="50%" valign="top">

### Professional library workspace

- Official beatmap cover thumbnails with graceful fallback
- Table columns for ID, title, artist, mapper and status
- Ruleset, play count, evidence and recovery state
- Search, filtering and bulk selection tools
- Installed, missing and selected summary cards

</td>
</tr>

<tr>
<td width="50%" valign="top">

### Safe local inspection

- Locate the osu!lazer storage directory
- Read `storage.ini` relocation information
- Compare discovered sets with local evidence
- Scan logs, replay metadata and previous manifests
- Keep the live database and hashed store read-only

</td>
<td width="50%" valign="top">

### Recovery output

- Download validated `.osz` files into a folder
- Produce a ZIP recovery bundle
- Export JSON and CSV manifests
- Generate SHA-256 checksums and a recovery report
- Hand validated packages to the osu!lazer client

</td>
</tr>
</table>

## Safety Principles

The most important project rule is simple:

> Beatmap Library Recovery must never write directly to osu!lazer's live `client.realm` database or content-addressed `files/` store.

The current implementation follows these safeguards:

- Local evidence is opened read-only or read from a user-selected copy.
- Downloads are staged outside the live osu!lazer store.
- HTML responses, empty files and damaged archives are rejected.
- Packages are checked for a valid `.osu` entry before use.
- SHA-256 hashes are generated for validated packages.
- Auto-import uses an external client handoff instead of database injection.
- Client secrets, tokens, cookies and private user files must never be committed.

## Application Workflow

### 1. Discover account history

Enter an osu! username or user ID. Development builds can use local OAuth client credentials or the built-in offline demo data.

### 2. Compare the local library

Optionally select the osu!lazer storage directory and run a read-only comparison. Maps already found locally are marked **In Library**.

### 3. Review and select beatmaps

Use the table to search and filter by:

- Beatmap ID
- Title
- Artist
- Mapper
- Ruleset
- Ranked status
- Minimum play count
- Installed state

### 4. Choose a recovery output

| Output | Description |
| --- | --- |
| Download to folder | Saves each validated `.osz` package separately. |
| ZIP recovery bundle | Creates one outer ZIP with packages, manifests, checksums and a report. |
| Auto-import | Passes each validated package to the installed osu!lazer client. |
| Manifest export | Records the selected set IDs, metadata and official URLs without importing. |

## Recovery Bundle Format

```text
recovery-bundle.zip
|-- beatmaps/
|   |-- <set-id> <artist> - <title>.osz
|-- manifest.json
|-- manifest.csv
|-- checksums.sha256
`-- recovery-report.txt
```

Each `.osz` remains a separate archive inside the outer ZIP.

## Development Setup

### Requirements

- Windows 10 or Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- Optional osu! OAuth application credentials for authenticated API testing

### Clone and build

```powershell
git clone <repository-url>
cd "Osu! Library Recovery"
dotnet restore
dotnet build Recovery.slnx
```

### Run the desktop app

```powershell
dotnet run --project src/Recovery.App
```

Enable **Use offline demo data** to inspect the interface without entering OAuth credentials.

### Run tests

```powershell
dotnet test Recovery.slnx
```

The current suite covers discovery, deduplication, filtering, local scanning, queue behavior, archive integrity, packaging and safety checks.

## OAuth Development Credentials

The osu! API uses OAuth. A public desktop executable cannot securely hide a static `client_secret`, so development builds currently accept contributor-provided credentials at runtime.

- Never commit OAuth credentials.
- Never place real credentials in screenshots, issues or logs.
- Never ask users for an osu! password or browser session cookie.
- A release authentication model still requires a security and privacy review.

## Technology Stack

<div align="center">

<p>
  <img src="https://skillicons.dev/icons?i=cs,dotnet,sqlite,github&theme=dark" alt="Core project technologies" />
</p>

</div>

| Technology | Purpose |
| --- | --- |
| C# | Primary application language |
| .NET 10 LTS | Runtime and self-contained publishing |
| Avalonia UI 12 | Cross-platform desktop UI and MVVM views |
| CommunityToolkit.Mvvm | Observable state and commands |
| SQLite | Planned persistent recovery-job state |
| HttpClient | osu! API access and streamed package retrieval |
| System.IO.Compression | Archive validation and ZIP bundle generation |
| xUnit | Automated unit and integration tests |

## Project Structure

```text
Beatmap Library Recovery/
|-- src/
|   |-- Recovery.App/          # Avalonia views and view models
|   |-- Recovery.Core/         # Domain models, filters and deduplication
|   |-- Recovery.Downloads/    # Download queue and archive validation
|   |-- Recovery.Import/       # Safe osu!lazer handoff
|   |-- Recovery.Local/        # Read-only local evidence scanners
|   |-- Recovery.OsuApi/       # Account discovery and API parsing
|   `-- Recovery.Packaging/    # Manifests, reports and ZIP bundles
|-- tests/
|   `-- Recovery.Tests/
|-- Osu.md                     # Product requirements document
|-- Recovery.slnx
|-- LICENSE
`-- README.md
```

## Development Status

| Area | Status |
| --- | --- |
| Modular .NET solution | Implemented |
| Discovery and deduplication | Implemented prototype |
| Modern library UI with thumbnails | Implemented |
| Local evidence scanning | Early implementation |
| Archive validation and checksums | Implemented |
| Folder and ZIP output | Implemented prototype |
| Auto-import handoff | Implemented prototype |
| Persistent crash-safe job resume | Planned |
| Approved production download route | Under review |
| Signed portable Windows EXE | Planned |
| Linux and macOS releases | Planned |

## Known Limitations

- Server-side play history may not contain offline-only or unsubmitted activity.
- A missing local database cannot be reconstructed byte for byte.
- The current source tree contains an experimental community download adapter. It is not an approved official osu! download route and must not be silently enabled in a public release.
- Auto-import currently confirms that the package was handed to the client, not that osu!lazer completed every import successfully.
- Local Realm inspection is schema-sensitive and remains optional.
- The single 32×32 application icon may appear soft at large Windows icon sizes.

## Roadmap

- [x] Phase 0 technical foundation
- [x] Online most-played discovery and deduplication
- [x] Read-only local evidence foundation
- [x] Archive integrity validation
- [x] Manifest and ZIP bundle generation
- [x] Professional library UI with cover thumbnails
- [ ] Persist recovery jobs in SQLite
- [ ] Resume interrupted work without redownloading verified packages
- [ ] Finalise a compliant package-download strategy
- [ ] Verify per-set auto-import completion
- [ ] Add file and folder pickers
- [ ] Publish a portable self-contained Windows executable
- [ ] Add code signing when project resources permit it
- [ ] Validate Linux and macOS packaging

The full requirements and delivery phases are documented in [Osu.md](Osu.md).

## Privacy and Repository Hygiene

The repository `.gitignore` excludes local credentials, osu! databases, replay files, beatmap packages, recovery staging data, generated manifests and build output.

Before opening an issue or sharing diagnostics, remove:

- OAuth client secrets and tokens
- Browser cookies
- Usernames when privacy is required
- Home-directory paths
- Private `.realm` databases
- `.osr` replay files
- Downloaded `.osz` packages
- User recovery manifests and logs

## Contributing

Contributions are welcome while the project is under active development.

1. Fork the repository.
2. Create a focused branch.
3. Make the required change.
4. Add or update tests.
5. Run `dotnet test Recovery.slnx`.
6. Open a pull request with a clear explanation and safety impact.

Use concise commit messages such as:

```text
feat: add persistent recovery job state
fix: reject malformed osz archives
docs: clarify OAuth development setup
test: cover interrupted download recovery
```

Do not include real beatmap packages, API credentials, user histories, production databases or copyrighted assets in tests or pull requests.

## Bug Reports

Include:

- Operating system and version
- Application version or commit
- Steps to reproduce
- Expected and actual behavior
- A redacted error message or log excerpt

Do not attach private credentials, database files, replay files, beatmap packages or unredacted user data.

## License

Beatmap Library Recovery is licensed under the [MIT License](LICENSE).

Beatmaps and osu! branding remain the property of their respective owners. This project does not grant permission to redistribute beatmap content.

## Developer

<div align="center">

### Built by TakanashiHaryth

<p>
  <a href="https://github.com/TakanashiHaryth">
    <img src="https://img.shields.io/badge/GitHub-TakanashiHaryth-181717?style=for-the-badge&logo=github&logoColor=white" alt="TakanashiHaryth on GitHub" />
  </a>
</p>

> “Life is like a GitHub repository. No progress happens until you make a commit.”

</div>

---

<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&color=gradient&customColorList=12,20,24&height=110&section=footer" width="100%" alt="Footer" />

### Thanks for visiting Osu! Library Recovery.

**Discover. Review. Recover.**

⭐ Star the repository if Mizuki is useful to your community.

</div>
