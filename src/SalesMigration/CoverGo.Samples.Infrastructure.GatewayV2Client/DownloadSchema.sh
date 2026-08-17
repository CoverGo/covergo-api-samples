#!/bin/sh
# Refresh schema.graphql from the V2 supergraph (quotation, payments, chParty and the
# other subgraphs).
#
# No post-processing: unlike the V1 gateway, the V2 supergraph introspects to a schema that
# is directly consumable -- it stitches no RequestManager surface and declares no interface
# without an implementing type.
#
# Review the resulting diff before committing -- the snapshot is checked in, matching the
# convention used across the CoverGo service repositories.
#
#   COVERGO_V2_URL=<url> COVERGO_TOKEN=<bearer> ./DownloadSchema.sh
set -eu

: "${COVERGO_V2_URL:?set COVERGO_V2_URL to the V2 supergraph URL}"
: "${COVERGO_TOKEN:?set COVERGO_TOKEN to a bearer token for that tenant}"

here=$(cd "$(dirname "$0")" && pwd)
root=$(cd "$here/../../.." && pwd)

cd "$root"
dotnet tool restore
dotnet dotnet-graphql download "$COVERGO_V2_URL" --token "$COVERGO_TOKEN" -f "$here/schema.graphql"

echo "Updated $here/schema.graphql"
