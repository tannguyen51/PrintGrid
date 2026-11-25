import os
from pathlib import Path

src = Path("src")
frontend = Path("frontend/src/features")
features = sorted([d.name for d in frontend.iterdir() if d.is_dir()])

controllers_path = src / "PrintGrid.Api" / "Controllers"
controllers = sorted([f.stem.replace("Controller","").replace("Ops","OPS ") for f in controllers_path.glob("*.cs") if f.is_file() and f.stem != "Startup"])

migrations_path = src / "PrintGrid.Infrastructure.Shared" / "Persistence" / "Migrations"
migration_files = sorted([f.stem for f in migrations_path.glob("*.cs") if f.is_file()], reverse=True)

print("FEATURES: %d" % len(features))
for f in features:
    print("  FE-" + f)
print("---CONTROLLERS: %d---" % len(controllers))
for c in controllers[:25]:
    print("  " + c)
print("---MIGRATIONS (first 5)---")
for m in migration_files[:5]:
    print("  " + m)
