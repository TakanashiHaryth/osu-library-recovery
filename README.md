<div align="center">

<!-- Anime Header -->

<img src="https://media.tenor.com/EEbyku4nU8gAAAAi/rimuru-spin.gif" width="170" alt="Anime Character"/>
<img src="https://media1.tenor.com/m/ajHV0O5APUMAAAAC/rimuru-rimuru-tempest.gif" width="170" alt="Anime Character"/>
<img src="https://media1.tenor.com/m/T6cnb8csQAMAAAAC/%E3%81%A1%E3%82%87%E3%81%93%E3%81%88%E3%81%84-chocoeiru.gif" width="170" alt="Anime Character"/>

<br>

<img src="https://readme-typing-svg.herokuapp.com?font=Orbitron&weight=700&size=28&duration=3200&pause=900&color=FF5C9D&center=true&vCenter=true&width=760&lines=Recover+your+osu!lazer+beatmap+library;Discover.+Review.+Validate.+Restore.;Safe+by+design.+Built+for+the+community." alt="Beatmap Library Recovery introduction" />

<br>

# 🌸 Beatmap Library Recovery

### 🔍 Account Discovery · 🛡️ Read-Only Safety · 📦 ZIP Bundling · 🚀 osu!lazer & Stable Handoff · 🐧 Linux Ready

<p>
  <img src="https://img.shields.io/badge/C%23-.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="C# and .NET 10" />
  <img src="https://img.shields.io/badge/Avalonia_UI-12.x-8B5CF6?style=for-the-badge&logo=avalonia&logoColor=white" alt="Avalonia UI 12" />
  <img src="https://img.shields.io/badge/osu!-lazer%20%7C%20Stable-FF66AA?style=for-the-badge&logo=osu&logoColor=white" alt="osu!lazer and osu! Stable" />
  <img src="https://img.shields.io/badge/Platform-Windows_%7C_Linux-0078D4?style=for-the-badge&logo=linux&logoColor=white" alt="Windows and Linux" />
</p>

<p>
  <img src="https://img.shields.io/badge/License-MIT-FF5C9D?style=flat-square" alt="MIT License" />
  <img src="https://img.shields.io/badge/Language-C%23%2014-512BD4?style=flat-square" alt="Language C# 14" />
  <img src="https://img.shields.io/badge/Status-Active_Development-52D6A0?style=flat-square" alt="Active Development Status" />
  <img src="https://img.shields.io/badge/Tests-32_Passing-52D6A0?style=flat-square" alt="32 Passing Tests" />
  <img src="https://img.shields.io/badge/Database_Safety-Read_Only-79A8FF?style=flat-square" alt="Read-Only Database Safety" />
  <img src="https://img.shields.io/badge/Version-v2.0.0-FF66AA?style=flat-square" alt="Version v2.0.0" />
</p>

</div>

---

> Current status: **Early Development Preview** — see [Osu.md](Osu.md) for requirements and roadmap.

> [!IMPORTANT]
> Beatmap Library Recovery is an independent community project. It is not affiliated with, endorsed by, or presented as an official product of osu! or ppy Pty Ltd.

> [!CAUTION]
> This repository is an early development preview. There is no official prebuilt release yet, and the experimental download strategy must be reviewed before public distribution.

---

## 🌌 About Beatmap Library Recovery

**Beatmap Library Recovery** is a safe, open-source desktop recovery workspace built with C# and Avalonia UI for **osu!lazer** and **osu! (Stable)** players across **Windows** and **Linux**.

It helps players rebuild a useful beatmap library after a reinstall, damaged database, failed storage move, or missing local records. The application discovers beatmap sets from available server-side play history, merges optional local evidence, deduplicates sets, validates packages, and safely hands them to the installed osu! client (or copies directly into osu! Stable's `Songs/` folder).

```text
╭──────────────────────────────────────────╮
│         RECOVERY WORKSPACE SYSTEM        │
├──────────────────────────────────────────┤
│  🔍 Account Play History Discovery       │
│  🛡️ 100% Read-Only Safety Protocol       │
│  📊 Deduplication & Metadata Merge       │
│  🎨 Rich Library UI with Beatmap Covers  │
│  📦 Validated ZIP Bundles & Manifests    │
│  🚀 Safe External Client Handoff         │
│     (osu!lazer & osu! Stable)            │
│  🐧 Cross-Platform (Windows & Linux)     │
╰──────────────────────────────────────────╯
```

> Beatmap Library Recovery reconstructs a playable beatmap library. It does **not** attempt to recreate the original `client.realm` database byte for byte.

```text
╭──────────────────────────────────────────╮
│       Discover Account History           │
│   Resolve username/ID & play history     │
╰────────────────────┬─────────────────────╯
                     │
                     ▼
╭──────────────────────────────────────────╮
│     Merge & Deduplicate Evidence         │
│ Compare local storage & deduplicate sets │
╰────────────────────┬─────────────────────╯
                     │
                     ▼
╭──────────────────────────────────────────╮
│    Review Covers, Metadata & State       │
│ Filter by ruleset, status & play count   │
╰────────────────────┬─────────────────────╯
                     │
                     ▼
╭──────────────────────────────────────────╮
│      Select Maps & Recovery Output       │
│ Folder download, ZIP bundle, or import   │
╰────────────────────┬─────────────────────╯
                     │
                     ▼
╭──────────────────────────────────────────╮
│    Validate Packages & Generate Report   │
│ SHA-256 checksums, manifest & log report │
╰──────────────────────────────────────────╯
```

---

## ✨ Main Features

<table>
<tr>
<td width="50%" valign="top">

### 🔍 Library Discovery

Discover beatmap history from online and local sources.

- Resolve an osu! username or numeric user ID
- Paginate through most-played history
- Merge recent scores when OAuth access is available
- Deduplicate difficulties by beatmap set ID
- Preserve play counts and evidence sources
- Built-in offline demo data for instant testing

</td>
<td width="50%" valign="top">

### 🎨 Library Workspace

Manage and inspect discovered beatmaps in a rich desktop UI.

- Official beatmap cover thumbnails with fallback
- Sortable table columns for ID, title, artist, mapper
- Ruleset, ranked status, play count and recovery state
- Real-time search and multi-criteria filtering
- Bulk selection and inverted selection tools
- Instant summary cards: Installed, Missing, Selected

</td>
</tr>

<tr>
<td width="50%" valign="top">

### 🛡️ Safe Local Inspection

Compare recovery sets against existing local installations.

- Target Client selection: easily toggle between **osu!lazer** and **osu! (Stable)**
- Auto-locate default osu!lazer and osu! (Stable) storage directories
- Scan osu! Stable `Songs/` folders and `.osu` files in read-only mode
- Parse `storage.ini` relocation configurations for lazer
- Compare discovered sets against local evidence
- 100% read-only access to Realm DB, hashed files, and Songs stores

</td>
<td width="50%" valign="top">

### 📦 Recovery Output

Flexible output targets for verified beatmap packages.

- Download validated `.osz` packages into a folder
- Package into a self-contained ZIP recovery bundle
- Export structured JSON and CSV manifests
- Generate SHA-256 checksums and text summary reports
- Safe client handoff directly to installed osu!lazer

</td>
</tr>
</table>

---

## 🔄 Application Workflow

### 1. Discover Account History

Enter an osu! username or user ID. Development builds can use local OAuth client credentials or the built-in offline demo data.

### 2. Compare the Local Library (Read-Only)

Optionally select the osu!lazer storage directory and run a read-only comparison. Maps already found locally are marked **In Library**.

### 3. Review and Select Beatmaps

Use the interactive recovery table to search, sort, and filter by:

- **Beatmap Set ID**
- **Title and Artist**
- **Mapper**
- **Ruleset** (osu!, osu!taiko, osu!catch, osu!mania)
- **Ranked status** (Ranked, Loved, Qualified, Pending, Graveyard)
- **Minimum play count**
- **Installed state** (Installed vs. Missing)

### 4. Choose a Recovery Output

| Output Mode             | Format / Destination      | Description                                                                                      |
| :---------------------- | :------------------------ | :----------------------------------------------------------------------------------------------- |
| **Download to folder**  | Directory of `.osz` files | Saves each validated `.osz` package separately into a chosen folder.                             |
| **ZIP recovery bundle** | Outer `.zip` archive      | Creates one outer ZIP with packages, manifests, checksums, and a recovery report.                |
| **Auto-import**         | Installed osu! client     | Passes packages to **osu!lazer** via safe OS handoff, or directly into **osu! Stable `Songs/`**. |
| **Manifest export**     | `.json` / `.csv`          | Records selected set IDs, metadata, and official URLs without downloading.                       |

---

## 🛡️ Safety Principles

The cardinal rule of Beatmap Library Recovery is:

> Beatmap Library Recovery must never write directly to osu!lazer's live `client.realm` database or content-addressed `files/` store.

The current implementation follows these strict safeguards:

- **Read-Only Access:** Local evidence is opened in strict read-only mode or read from a user-selected copy.
- **Isolated Staging:** Downloads and temporary files are staged outside the live osu!lazer store.
- **Archive Validation:** HTML responses, empty files, and damaged archives are rejected.
- **Format Inspection:** Packages are checked for a valid `.osu` entry before use.
- **Cryptographic Verification:** SHA-256 hashes are generated for all validated packages.
- **External Client Handoff:** Auto-import uses an external client handoff instead of database injection.
- **Credential Hygiene:** Client secrets, tokens, cookies, and private user files must never be committed.

---

## 📦 Recovery Bundle Specification

When creating a **ZIP recovery bundle**, the application generates a structured, portable archive:

```text
recovery-bundle.zip
├── beatmaps/
│   ├── <set-id> <artist> - <title>.osz
│   └── ...
├── manifest.json        # Machine-readable recovery metadata
├── manifest.csv         # Spreadsheet-friendly list of recovered sets
├── checksums.sha256     # SHA-256 hashes for all bundled .osz packages
└── recovery-report.txt  # Human-readable summary of the recovery run
```

Each beatmap set remains an independent `.osz` archive inside the `beatmaps/` directory, allowing manual extraction or direct drag-and-drop into osu!lazer at any time.

---

## 🧰 Technology Stack

<div align="center">

### Core Development

<p>
  <img src="https://skillicons.dev/icons?i=cs,dotnet,sqlite,github&theme=dark" alt="Core Technologies"/>
</p>

### Frameworks & Architecture

<p>
  <img src="https://img.shields.io/badge/Avalonia_UI-12.x-8B5CF6?style=for-the-badge&logo=avalonia&logoColor=white" alt="Avalonia UI 12"/>
  <img src="https://img.shields.io/badge/CommunityToolkit-MVVM-512BD4?style=for-the-badge" alt="CommunityToolkit MVVM"/>
  <img src="https://img.shields.io/badge/osu!-lazer_Client-FF66AA?style=for-the-badge&logo=osu&logoColor=white" alt="osu!lazer"/>
  <img src="https://img.shields.io/badge/xUnit-Testing-512BD4?style=for-the-badge" alt="xUnit"/>
</p>

</div>

| Technology                | Purpose                                                     |
| :------------------------ | :---------------------------------------------------------- |
| **C# 14**                 | Primary application language                                |
| **.NET 10 LTS**           | Modern runtime and self-contained compilation target        |
| **Avalonia UI 12**        | High-performance cross-platform desktop UI framework        |
| **CommunityToolkit.Mvvm** | Observable state and async command bindings                 |
| **SQLite**                | Planned persistent recovery-job state and resume support    |
| **HttpClient**            | Asynchronous osu! API access and streamed package retrieval |
| **System.IO.Compression** | Archive validation and ZIP bundle generation                |
| **xUnit**                 | Automated unit and integration test suite                   |

---

## 📋 Requirements

Before building or running Beatmap Library Recovery, make sure your system has:

- **Windows:** Windows 10 (version 1809+) or Windows 11
- **Linux:** Any modern distribution (Ubuntu/Debian, Fedora, Arch Linux, SteamOS / Steam Deck) with standard desktop libraries (`libfontconfig1`, `libX11` / Wayland)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (v10.0.100 or newer)
- Git
- _(Optional)_ osu! OAuth application credentials for live API synchronization

### Check .NET SDK

```bash
dotnet --version
```

### Check Git

```bash
git --version
```

---

## 🚀 Installation & Build

### 1. Clone the Repository

```bash
git clone https://github.com/TakanashiHaryth/osu-library-recovery.git "Osu! Library Recovery"
cd "Osu! Library Recovery"
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build the Solution

```bash
dotnet build Recovery.slnx
```

### 4. Run Automated Tests

```bash
dotnet test Recovery.slnx
```

The current suite covers discovery, deduplication, filtering, local scanning, queue behavior, archive integrity, packaging, client handoff, and cross-platform safety checks (**32 passing tests**).

### 5. Launch the Desktop Application

```bash
dotnet run --project src/Recovery.App
```

### 6. Portable Single-File Executables

#### Windows Portable:

Double-click `publish-portable.bat` or run:

```powershell
dotnet publish src/Recovery.App/Recovery.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "./publish/Osu_Library_Recovery"
```

#### Linux Portable (x64 / SteamOS):

Run `publish-linux.sh` (or `publish-linux.bat` from Windows):

```bash
./publish-linux.sh
# To execute on Linux:
chmod +x ./publish/Osu_Library_Recovery_Linux/Osu_Library_Recovery
./publish/Osu_Library_Recovery_Linux/Osu_Library_Recovery
```

### 7. Offline Demo Mode & OAuth Credentials

- **Offline Demo Mode:** Check **"Use offline demo data"** in the recovery panel to inspect and test the interface immediately without needing an internet connection or OAuth credentials.
- **Live osu! API Access:** To test against live account data, provide contributor-level OAuth credentials (`Client ID` and `Client Secret`) from your [osu! account settings](https://osu.ppy.sh/home/account/edit#oauth).

> [!NOTE]
> Never commit OAuth credentials. Never place real credentials in screenshots, issues, or logs. Never ask users for an osu! password or session cookie.

---

## 🐧 Linux & Steam Deck Support

Beatmap Library Recovery provides first-class Linux support:

- **osu!lazer Flatpak:** Automatically detects `~/.var/app/sh.ppy.osu/data/osu`.
- **osu!lazer Native / AppImage:** Automatically detects `~/.local/share/osu` and binary in `$PATH`.
- **osu! (Stable) via Wine / Proton:** Supports Wine prefixes (`~/.wine/drive_c/osu!`), Bottles, and Lutris setups with automatic copying of `.osz` files into the `Songs/` folder.
- **Wayland & X11:** Native hardware-accelerated rendering powered by Avalonia UI.

---

## 📁 Project Structure

```text
Beatmap Library Recovery/
├── docs/                   # Architecture spikes and technical reports
│   └── phase-0-spike-report.md
├── src/
│   ├── Recovery.App/       # Avalonia views, view-models and UI assets
│   ├── Recovery.Core/      # Domain models, filter logic and deduplication
│   ├── Recovery.Downloads/ # Managed download queue and archive validation
│   ├── Recovery.Import/    # Safe external osu!lazer handoff
│   ├── Recovery.Local/     # Read-only local evidence scanners
│   ├── Recovery.OsuApi/    # Account discovery and osu! API v2 client
│   └── Recovery.Packaging/ # Manifests, reports and ZIP bundle generation
├── tests/
│   └── Recovery.Tests/     # Automated xUnit test suite (25 tests)
├── Osu.md                  # Product requirements document (PRD)
├── Recovery.slnx           # .NET 10 solution file
├── LICENSE                 # MIT License
└── README.md
```

---

## 📊 Development Status

| Area / Capability                      | Status          | Description                                             |
| :------------------------------------- | :-------------- | :------------------------------------------------------ |
| **Modular .NET solution**              | ✅ Implemented  | Multi-project architecture on .NET 10 LTS               |
| **Discovery and deduplication**        | ✅ Implemented  | Online most-played history and difficulty deduplication |
| **Modern library UI with thumbnails**  | ✅ Implemented  | Table view with cover art, status badges, and filtering |
| **Local evidence scanning**            | ✅ Implemented  | Read-only discovery for osu!lazer & osu! (Stable)       |
| **Archive validation and checksums**   | ✅ Implemented  | `.osu` archive validation and SHA-256 calculation       |
| **Folder and ZIP output**              | ✅ Implemented  | Multi-target export with manifests and reports          |
| **Auto-import handoff**                | ✅ Implemented  | osu!lazer client handoff & osu! Stable Songs/ import    |
| **Linux & Steam Deck support**         | ✅ Implemented  | Native, Flatpak, AppImage, and Wine/Proton detection    |
| **Portable single-file binaries**      | ✅ Implemented  | Self-contained executables for Windows and Linux        |
| **Persistent crash-safe job resume**   | ⏳ Planned      | SQLite-based state storage for recovery jobs            |
| **Approved production download route** | 🔍 Under Review | Reviewing compliant download providers                  |

---

## 📋 Changelog

All version updates and release notes are tracked in **[CHANGELOG.md](CHANGELOG.md)**:

- **v2.0.0:** Added full Linux support (Flatpak, AppImage, Wine), osu! Stable direct import, Windows & Linux portable single-file executables, UI updates, and package validation.
- **v1.0.0:** Initial project release.

---

## ⚠️ Known Limitations

- **Server History Bounds:** Server-side play history may not contain offline-only or unsubmitted activity.
- **Database Reconstruction:** The application reconstructs a playable beatmap library; it does **not** restore user scores, replays, or the exact binary `client.realm` state.
- **Experimental Downloader:** The current source tree contains an experimental community download adapter. It is not an approved official osu! download route and must not be silently enabled in a public release.
- **Auto-import Confirmation:** Auto-import currently confirms that the package was handed to the client, not that osu!lazer completed every import successfully.
- **Local Realm Inspection:** Local Realm inspection is schema-sensitive and remains optional.
- **Icon Resolution:** The single 32×32 application icon may appear soft at large Windows icon sizes.

---

## 🔐 Security and Privacy

Beatmap Library Recovery handles local game data, account histories, and network downloads. Security and privacy must not be treated as optional.

### Protected Files

The repository `.gitignore` excludes local credentials, osu! databases, replay files, beatmap packages, recovery staging data, generated manifests, and build output:

```gitignore
*.realm
*.realm.lock
*.realm.management/
*.osr
*.osz
recovery-bundle*.zip
staging/
manifest.json
manifest.csv
checksums.sha256
recovery-report.txt
*.user
appsettings.Development.json
```

### Database & Storage Integrity

- **Never Modifies Live Realm:** The live `client.realm` database and `files/` store are strictly read-only.
- **No Database Injection:** The tool never writes rows or alters tables inside the Realm database.
- **Isolated Staging:** All downloads and temp files are isolated outside the osu!lazer store.

### API Credentials & Privacy Hygiene

Before opening an issue or sharing diagnostics, ensure you remove:

- OAuth client secrets and access tokens
- Browser session cookies
- Personal usernames when privacy is required
- User home-directory paths
- Private `.realm` database files
- `.osr` replay files
- Downloaded `.osz` packages
- Recovery manifests, checksums, and personal logs

---

## ⚠️ osu! Community Notice

This project is an **independent, open-source community tool**.

- It is not affiliated with, endorsed by, or sponsored by **ppy Pty Ltd** or **osu!**.
- "osu!" and related marks belong to **ppy Pty Ltd**.
- Beatmaps, audio, and background artwork are the property of their respective creators and artists.
- This project does not grant permission to redistribute copyrighted beatmap content.
- Always respect rate limits and follow osu! terms of service.

---

## 🗺️ Development Roadmap

- [x] Phase 0 technical foundation
- [x] Online most-played discovery and deduplication
- [x] Read-only local evidence foundation
- [x] Archive integrity validation
- [x] Manifest and ZIP bundle generation
- [x] Professional library UI with cover thumbnails
- [x] Safe external osu!lazer auto-import handoff
- [x] Direct osu! (Stable) `Songs/` auto-import handoff
- [x] Full Linux & Steam Deck support (Flatpak, AppImage, Wine/Proton)
- [x] Portable self-contained single-file releases (Windows & Linux)
- [ ] Persist recovery jobs in SQLite
- [ ] Resume interrupted work without redownloading verified packages
- [ ] Finalise a compliant package-download strategy
- [ ] Verify per-set auto-import completion
- [ ] Add file and folder pickers
- [ ] Add code signing when project resources permit it

The full requirements and delivery phases are documented in [Osu.md](Osu.md). Technical-spike findings are recorded in [docs/phase-0-spike-report.md](docs/phase-0-spike-report.md).

---

## 🤝 Contributing

Contributions are welcome while the project is under active development.

### Contribution Workflow

1. Fork the repository
2. Create a focused branch (`feature/your-feature-name`)
3. Make the required changes
4. Add or update unit tests
5. Run `dotnet test Recovery.slnx` to verify everything passes
6. Commit changes with clear messages
7. Push the branch to your fork
8. Open a Pull Request with a clear explanation and safety impact

```powershell
git checkout -b feature/your-feature-name
git add .
git commit -m "feat: add persistent recovery job state"
git push origin feature/your-feature-name
```

Use concise conventional commit messages:

```text
feat: add persistent recovery job state
fix: reject malformed osz archives
docs: clarify OAuth development setup
test: cover interrupted download recovery
```

> [!CAUTION]
> Do not include real beatmap packages, API credentials, user histories, production databases, or copyrighted assets in tests or pull requests.

---

## 🐛 Bug Reports

When reporting a bug, include:

- Operating system and version (e.g., Windows 11 23H2)
- Application version or git commit hash
- Steps to reproduce the problem
- Expected and actual behavior
- A redacted error message or log excerpt

**Do not include:**

- osu! passwords, OAuth client secrets, or tokens
- Local `client.realm` files or `.osr` replay files
- Downloaded `.osz` packages or personal user data

---

## 📜 License

Beatmap Library Recovery is licensed under the [MIT License](LICENSE).

```text
MIT License

Copyright (c) 2026 TakanashiHaryth

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
...
```

Beatmaps and osu! branding remain the property of their respective owners. This project does not grant permission to redistribute beatmap content.

---

## 👨‍💻 Developer

<div align="center">

### Built by TakanashiHaryth

<p>
  <a href="https://github.com/TakanashiHaryth">
    <img src="https://img.shields.io/badge/GitHub-TakanashiHaryth-181717?style=for-the-badge&logo=github&logoColor=white" alt="GitHub"/>
  </a>
</p>

> “Life is like a GitHub repository. No progress happens until you make a commit.”

</div>

---

<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&color=gradient&customColorList=12,20,24&height=120&section=footer" width="100%" alt="Footer"/>


### Thanks for visiting Beatmap Library Recovery.

**Discover · Review · Validate · Restore**

⭐ Star the repository if this tool helps you recover your beatmap library!

</div>
