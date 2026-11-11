#!/bin/sh
set -eu
APPDIR=/opt/cura/runtime
export APPDIR
export LD_LIBRARY_PATH="$APPDIR:$APPDIR/runtime/compat:$APPDIR/usr/lib/x86_64-linux-gnu:$APPDIR/lib/x86_64-linux-gnu:$APPDIR/usr/lib${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
export PATH="$APPDIR:$APPDIR/usr/bin:$PATH"
exec "$APPDIR/CuraEngine" "$@"
