# CuraEngine local test installation

Installed portable runtime (no Cura GUI, file associations or system-wide PATH changes):

```text
.local/cura-5.11.0/CuraEngine.exe
.local/python312/python.exe
```

The Cura Windows installer was downloaded from the official release:
https://github.com/Ultimaker/Cura/releases/tag/5.11.0

- File: `UltiMaker-Cura-5.11.0-win64-X64.exe`
- SHA-256: `294d4c3907137e789b16b15990bab64380eff5df7c7d9d0d06ec83966f36ffa0`
- Authenticode verified: **Valid**, signer **Ultimaker B.V.**
- Extracted with official 7-Zip 25.01, not run as a GUI installer.
- CuraEngine reports version **5.11.0**.
- Profile-generation helper: official Python **3.12.10** Windows embeddable package.

Profiles are generated from the shipped `fdmprinter.def.json` and
`fdmextruder.def.json`, evaluating dependency formulas rather than passing Python
expressions to CuraEngine. Each file has **720 resolved settings per stack**, with
one global stack and four virtual extruder stacks. No formula evaluation failed
or fell back to a circular dependency when generating these two profiles.

These are **virtual local-test rigs**, not manufacturer profiles for actual lab
hardware. Four virtual tools let the quote adapter be exercised with several
colors on one model. They do not simulate a specific AMS/MMU or certify a
physical printer. Do not send the generated G-code to real equipment.

| Setting | Standard 220 | Fast 320 |
|---|---|---|
| File | `local-standard-pla.resolved.json` | `local-fast-pla.resolved.json` |
| Machine ID | `11111111-1111-4111-8111-111111111101` | `22222222-2222-4222-8222-222222222202` |
| Build volume | 220 × 220 × 250 mm | 320 × 320 × 350 mm |
| Material | PLA, 1.75 mm, 1.24 g/cm³ | same |
| Nozzle | 0.4 mm | 0.4 mm |
| Layer | 0.2 mm | 0.2 mm |
| Print / outer wall / travel | 50 / 25 / 120 mm/s | 90 / 40 / 180 mm/s |
| Print / travel acceleration | 500 / 1000 mm/s² | 1500 / 2500 mm/s² |
| Nozzle / first layer | 205 / 210 °C | same |
| Bed | 60 °C | 60 °C |
| Walls / top / bottom | 3 walls / 1 mm / 1 mm | same |
| Base infill | grid, 20% | same |
| Support | zigzag, 15%, angle 55° | same |
| Retraction | 1.2 mm at 35 mm/s | same |
| Adhesion | 4 mm brim | same |
| Multi-color purge | prime tower, 18 mm, min 15 mm³ | same |
| Demo hourly rate | 6,000 VND/hour | 7,500 VND/hour |
| Demo failure rate | 8% | 5% |

Profile rates are example contracts for testing; they are not actual lab quotes.
The adapter applies customer layer/infill overrides to both global and extruder
stacks, updating infill spacing and layer-dependent thickness/counts as well.
The profile's tested scope is 1–4 colors; 5+ selected colors are rejected.

## Run now, no Docker/database needed

From the repository root:

```powershell
dotnet run --project tools/LocalSlicerTest -- E:\Project\PrintGrid
```

This calls the **real installed CuraEngine**, not a mock, for two machines × two
fixtures × two sizes (original and M), plus two checks that changing infill and
layer height changes the actual engine output (**10 checks total**). Results are saved to
`.local/slicer-smoke-results.json`. Fixture files for upload:

- `tests/manual/slicer/cube-20mm.stl`: one color.
- `tests/manual/slicer/three-color-30x20x20mm.3mf`: three closed material regions.
  Select colors in this order: BLUE, WHITE, SIGNALRED.

`M` rescales the longest edge to **200 mm**. Use ORIGINAL to keep the small sample
dimensions. Large size changes material and time substantially.

## Test through the website

Docker Desktop must be running. The setup deliberately uses the separate
`printgrid_slicer_local` database, never a production/remote database. No existing
lab or customer rows in `printgrid_db` are changed.

```powershell
docker compose up -d postgres redis minio
pwsh -NoProfile -File tools/local-slicer/Setup-LocalSlicer.ps1 -SeedLocalDatabase
dotnet run --project src/PrintGrid.Api --launch-profile http
```

In another terminal:

```powershell
cd frontend
npm run dev
```

Open http://localhost:5173. The seed applies migrations and adds 2 test labs,
matching machine IDs, abundant stock for the supported colors and demo prices.
All three accounts below use password **`PrintGridLocal!2026`**, only in this
isolated local test DB:

| Account | Role |
|---|---|
| `slicer-customer@printgrid.local` | Customer |
| `slicer-ops@printgrid.local` | OpsManager |
| `slicer-admin@printgrid.local` | Admin |

1. Sign in as customer and upload a fixture.
2. Sign in as Ops in a separate browser session; approve it at `/model-reviews`.
3. Return to customer: choose PLA, color(s), size, quantity and polishing, then quote.

SMTP and automatic payment confirmation are disabled in local settings.
`appsettings.Local.json` is loaded only in Development, gitignored and excluded
from Docker builds. Existing environment variables/CLI arguments take precedence.
The setup reads Docker-local credentials from `.env` without printing them. For
another local PostgreSQL credential, set `PRINTGRID_SLICER_LOCAL_DB` (must still
use loopback and database `printgrid_slicer_local`).

Reconfigure profiles without DB access:

```powershell
pwsh -NoProfile -File tools/local-slicer/Setup-LocalSlicer.ps1 -SmokeTest
```

To resume the previous local database later, remove only the
`ConnectionStrings.DefaultConnection` override from `appsettings.Local.json`.
Keep the engine/profile paths if you still want them. Nothing modifies production.

## Rebuild the portable installation elsewhere

Download the pinned official Cura installer above, verify its SHA-256 and signer,
then extract its full contents to `.local/cura-5.11.0` with 7-Zip (keep required
DLLs next to CuraEngine). Extract
https://www.python.org/ftp/python/3.12.10/python-3.12.10-embed-amd64.zip
to `.local/python312`, then run the setup script. Retain the licenses shipped in
the Cura distribution; CuraEngine is AGPLv3, with other bundled component licenses.

## Deploying to a server

CuraEngine runs on the API host. Installing it on a developer's Windows PC does
not install it on a deployed server, and customer/lab PCs do not need it for quoting.

- **Windows host:** install the matching CuraEngine runtime and its dependencies
  on that host, copy the resolved profiles, and configure paths accessible to the
  API service account.
- **Linux/Docker host:** the API Dockerfile downloads the official Cura 5.11.0
  Linux AppImage, verifies SHA-256, extracts the complete runtime and copies the
  pinned resource library from `infra/slicer/cura-resources/5.11.0`. The engine
  and definitions therefore cannot silently drift to different versions.

Labs select a stable Cura definition ID from `GET /api/v1/cura-library/machines`.
After machine creation the API resolves definition, extruder, material, variant
and quality inheritance, smoke-slices a closed mesh, stores verified profiles in
the database and rematches the machine. Exact verified matches can be auto-approved;
partial or failed matches stay `PendingReview` or `MissingProfile`.

`.local/`, `.env`, database dumps and `appsettings.Local.json` are excluded from
Git/Docker. Production does not load `appsettings.Local.json`; configure
`QuoteSlicer` through deployment environment variables or production settings.
The container configures these defaults; they can be overridden if paths change:

```text
QuoteSlicer__ExecutablePath=/opt/cura/CuraEngine
CuraLibrary__ResourcesPath=/opt/cura/resources
CuraLibrary__ResolverPath=/opt/cura/tools/resolve_profile.py
```

Admin can run an idempotent database synchronization with
`POST /api/v1/cura-library/sync`. The endpoint resolves and smoke-tests profiles for
non-approved configured FDM machines, reuses identical active profiles, and never
replaces profile IDs already attached to an `Approved` machine. A lab/ops user can
still synchronize one machine through
`POST /api/v1/cura-library/machines/{machineId}/sync`.

These remain virtual test profiles and example rates. Add the other required
colors and the second machine's configuration as needed. The corresponding
machine/lab rows, inventory and commercial rates must also exist in the target
database. Local database changes (including assigning machines to an existing
lab) are not transferred by `git push`; the schema migration does not seed them.
