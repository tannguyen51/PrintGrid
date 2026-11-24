#!/bin/sh
set -eu
# CLI does not display a window. Xvfb satisfies the distribution's wx/GTK runtime.
export LIBGL_ALWAYS_SOFTWARE=1
exec xvfb-run -a -e /dev/null /usr/bin/prusa-slicer "$@"
