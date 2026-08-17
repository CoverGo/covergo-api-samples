# Contributing

Thanks for helping improve these samples. They exist to make a real integration easier, so
the bar is "would this have saved someone a day" rather than "is it clever".

By participating you agree to the [Code of Conduct](CODE_OF_CONDUCT.md).

## Before you start

Open an issue first for anything beyond a typo or an obvious fix. A sample is only worth
adding if it calls a real CoverGo API and has been run against a real tenant — see
[Verify against a tenant](#verify-against-a-tenant).

## Never commit a credential

This repository is public.

- `appsettings.json` carries **placeholder values only** — obviously fake, and documented as
  such. Real client ids, secrets and tenant URLs are supplied at run time through
  `dotnet user-secrets` or environment variables.
- Never commit a token, a customer record, or an internal hostname.
- Secret scanning and push protection are enabled, and CI fails on a detected credential.
  Treat a push-protection block as correct until you have proved otherwise.

If a credential is ever committed, say so immediately and get it rotated. Removing the file
in a later commit does not remove it from history.

## Building

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) and Node 24 or newer.

```bash
npm ci            # installs Nx and the .NET plugin
npm run build     # nx run-many -t build
npm run lint      # nx run-many -t lint   (dotnet format --verify-no-changes)
npm run test      # nx run-many -t test
```

Projects and targets are inferred by [`@nx-dotnet/core`](https://github.com/nx-dotnet/nx-dotnet)
from the `.csproj` files, including the dependency graph, so adding a project needs no Nx
configuration.

Nx is pinned to 22.x on purpose: the .NET plugin's newest release declares
`nx < 23.0.0`, and running an unsupported pairing is the worse trade in a repository other
people clone. Do not bump it past that without checking the plugin's peer range.

Warnings are errors. That is deliberate — it has caught real defects here, including an async
iterator that silently dropped its cancellation token.

## Verify against a tenant

Unit tests prove the mapping; only a real call proves the sample. Run it against a CoverGo
tenant before opening a pull request, and say in the description what you ran and what came
back. Every defect worth fixing in this repository so far was found by running it, not by
reading it.

Re-run it a second time as well. A sample that duplicates records on a re-run is broken:
CoverGo has no upsert for most entities, so a loader must read before it writes.

## Commits and pull requests

- Work is tracked in **GitHub issues on this repository** — never an internal CoverGo tracker.
  An internal ticket key identifies the client it was raised for, so it does not belong in a
  commit, a branch name, a pull request, or a code comment here.
- Conventional commits: `type(scope): subject (#issue)` — for example
  `feat(samples): add the Agent migration input (#1)`. The issue number goes at the end, not
  the front: git strips a line beginning with `#` as a comment, so a leading `#1` silently
  deletes your subject line.
- One logical change per commit. Squash fixups before review.
- Explain **why** in the body, not what the diff already shows. If you chose one approach over
  another, record the trade-off; the next person will otherwise re-litigate it.
- Say plainly what you did not do. A known gap that is written down is useful; one that is
  discovered later is not.

## Regenerating a GraphQL schema

`schema.graphql` snapshots are committed, matching the convention across the CoverGo service
repositories. Refresh one with the `DownloadSchema.sh` beside it rather than by hand, and
review the diff before committing — the V1 script also runs a strip step that is required,
not cosmetic.
