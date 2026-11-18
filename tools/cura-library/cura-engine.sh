#!/bin/sh
set -eu
APPDIR=/opt/cura/runtime
export APPDIR
export LD_LIBRARY_PATH="$APPDIR:$APPDIR/runtime/compat:$APPDIR/usr/lib/x86_64-linux-gnu:$APPDIR/lib/x86_64-linux-gnu:$APPDIR/usr/lib${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
export PATH="$APPDIR:$APPDIR/usr/bin:$PATH"
# The released AppImage binary names its ELF interpreter relative to its working
# directory (lib64/ld-linux-x86-64.so.2). Invoke the host loader explicitly so API
# calls from /app or a temporary slicing directory work without changing cwd.
exec /lib64/ld-linux-x86-64.so.2 "$APPDIR/CuraEngine" "$@"
