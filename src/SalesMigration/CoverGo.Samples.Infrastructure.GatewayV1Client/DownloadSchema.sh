#!/bin/sh
# Refresh schema.graphql from the V1 gateway (stitches ChannelManagement, Users, Policies).
#
# Downloads the schema by introspection, then runs strip-schema.py beside this script. The
# strip is required, not cosmetic: a schema introspected from a deployed gateway is not
# directly consumable for client generation. See strip-schema.py for what it removes and why.
#
# Review the resulting diff before committing -- the snapshot is checked in, matching the
# convention used across the CoverGo service repositories.
#
#   COVERGO_V1_URL=<url> COVERGO_TOKEN=<bearer> ./DownloadSchema.sh
set -eu

: "${COVERGO_V1_URL:?set COVERGO_V1_URL to the V1 gateway URL}"
: "${COVERGO_TOKEN:?set COVERGO_TOKEN to a bearer token for that tenant}"

here=$(cd "$(dirname "$0")" && pwd)
root=$(cd "$here/../../.." && pwd)

cd "$root"
dotnet tool restore
dotnet dotnet-graphql download "$COVERGO_V1_URL" --token "$COVERGO_TOKEN" \
  -f "$here/schema.downloaded.graphql"

python3 "$here/strip-schema.py" "$here/schema.downloaded.graphql" "$here/schema.graphql"

rm -f "$here/schema.downloaded.graphql"
echo "Updated $here/schema.graphql"
