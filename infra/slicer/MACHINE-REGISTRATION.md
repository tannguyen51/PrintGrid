# Machine registration and profile library

Lab UI: `/lab/machines`. Admin UI: `/admin/cura-profiles`.

Each machine now records manufacturer/model, build dimensions, layer/tolerance limits,
supported materials, physical extruders, maximum colors, nozzle/filament diameters,
heated-bed capability, maximum temperatures, hourly rate and failure fraction.
Inventory and color stock remain attached to the lab, not duplicated per machine.

The library stores immutable resolved Cura JSON in PostgreSQL `scheduling.cura_profiles`.
Profiles cover one material and one hardware configuration. There is no runtime internet
lookup: automatic matching searches the profiles imported by administrators.

Registration matches manufacturer/model (trimmed and case insensitive), dimensions,
diameters, physical extruders, maximum colors, heated bed, thermal limits and engine
version. All declared materials must have a compatible profile. If multiple versions
match, the latest imported version is chosen. Admin can view and edit an existing JSON
to create a new version; already approved machines retain their selected versions.

- `MissingProfile`: machine is saved, but cannot quote or receive new FDM work.
- `PendingReview`: complete profile coverage found; still cannot quote or receive work.
- `Approved`: admin reviewed the configuration; eligible subject to existing lab,
  machine-status, stock, size and capacity checks.
- `Rejected`: admin rejected/revoked approval, with a reason.

Editing specifications invalidates approval and matches again. Importing a profile
automatically rematches missing/pending machines. Rejected machines require an explicit
Admin "Tìm lại profile" before review. Operational Idle/Offline/Maintenance states do not
override the independent profile approval state.

Admin imports resolved JSON containing `global` and consecutive `extruder.N` objects.
The importer checks dimensions, diameters, tool coverage, temperatures and essential
speed/acceleration settings. The current runner supports rectangular FDM beds and tools
with a common nozzle/filament diameter. Cura GUI `.curaprofile` archives and unresolved
`.def.json` inheritance trees must be resolved before import. This structural validation
does not certify physical print quality; Admin review remains required.

Runtime config:

```
QuoteSlicer__ExecutablePath=/opt/cura/CuraEngine
QuoteSlicer__EngineVersion=5.11.0
QuoteSlicer__TimeoutSeconds=120
```

Apply migration `AddMachineCuraProfileRegistry` before starting the updated API.
Old machine-ID/path mappings no longer authorize quotation. Existing FDM machines must
have hardware configuration, matching library profiles and approval. Profile JSON and
machine links persist in PostgreSQL; adding profiles requires no deployment. The actual
CuraEngine executable still needs to be installed in the backend runtime.

For the two existing **virtual local test machines only**, import their bundled files
and populate missing hardware settings with:

```
dotnet run --project tools/LocalSlicerTest -- E:/Project/PrintGrid --import-current-profiles
```

This command is restricted to loopback PostgreSQL. It leaves matching machines pending
Admin review. The two bundled profiles are synthetic test configurations, not certified
profiles for physical printers.

Verification against an API started with the isolated `printgrid_slicer_local` database:

```
dotnet run --project tools/LocalSlicerTest -- E:/Project/PrintGrid --registry-api-smoke
```

This creates and removes a temporary local admin, lab, machine and profile. It tests
registration/matching, approval, invalidation after edits, blocking approval without a
profile, automatic rematching on profile import and rejection.
