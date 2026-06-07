#!/bin/sh
set -e

# Placeholder for runtime configuration (e.g. injecting the API base URL into
# a generated env.js). Currently the Angular environment is baked at build time,
# so we simply hand off to the CMD (nginx).
echo "Lus.UI (shiftiz.com) starting nginx..."

exec "$@"
