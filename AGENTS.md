# Agent instructions

Guidance for AI coding agents working in this repository. `CLAUDE.md` and
`.github/copilot-instructions.md` are symlinks to this file — edit this one.

## What this repository is

Reference C# programs showing how to call the CoverGo APIs. It is **not** a supported SDK, and
it is **public**. Both facts shape every change:

- A reader is a customer developer, not a CoverGo engineer. Explain the constraint CoverGo
  enforces, not just the syntax.
- Nothing internal belongs here, and no CoverGo client is ever named. See the rules below.

## Hard rules

**Never commit a secret.** `appsettings.json` carries obviously-fake placeholders. Real values
come from `dotnet user-secrets` or the environment at run time. Secret scanning and push
protection are on; a block is correct until proven otherwise.

**Never name a CoverGo client.** Not the client, not their people, not their tenant, project or
ticket keys, not the integration they asked for, not a detail that identifies them by
elimination. This holds in code, comments, commit messages, sample data, issues and pull
requests alike. A CoverGo relationship is confidential, and this repository is public: write
every sample as if no particular client existed. Invented sample data must be obviously
invented, and must not echo a real client's name.

**Nothing else internal either.** No internal hostnames or cluster URLs, no internal tracker
keys, no credentials, no real personal data. Work here is tracked in GitHub issues on this
repository; reference the issue number at the **end** of the commit subject —
`feat(samples): … (#1)`, never an internal key. It goes at the end because git strips a line
beginning with `#` as a comment.

These two are the easiest rules to break by accident. Re-read the diff for them before every
commit: a public commit stays reachable by its SHA even after a force-push, so a slip is not
fully undoable.

**Verify before claiming.** Do not assert that an API behaves a certain way, that a package
version is current, or that a call succeeds, without checking. Run it, read the source, or
query the registry.

**Warnings are errors.** `TreatWarningsAsErrors` is on. Do not relax it to make a build pass.

## Toolchain

.NET 10 SDK and Node 24 or newer. Nx drives the build; projects and targets are inferred by
`@nx-dotnet/core` from the `.csproj` files, so adding a project needs no Nx configuration.

```bash
npm ci
npm run build     # nx run-many -t build
npm run lint      # nx run-many -t lint
npm run test      # nx run-many -t test
```

Nx is pinned to 22.x on purpose: `@nx-dotnet/core` declares `nx < 23.0.0`. Do not bump Nx past
that without checking the plugin's peer range first.

## Layout and architecture

One folder per sample under `src/`, each following Clean Architecture:

- **Domain** — the record a customer maps their own data onto, and the interface they implement
  to supply it. No dependencies.
- **Application** — use cases and the ports they call. Depends on Domain only.
- **Infrastructure** — configuration, authentication, mapping, and the generated GraphQL
  clients. The only layer that may know CoverGo speaks GraphQL.

Keeping the gateway clients out of Domain and Application is the point: it is what lets a
customer swap in their own data source without touching GraphQL. Never reference a gateway
client from an inner layer.

## Working with the CoverGo APIs

**Validation errors can arrive on an HTTP 200.** Mutations return them inside the payload's
`errors` union; some rejections instead come back as top-level GraphQL errors. Check both, and
attribute a per-record failure to that record rather than aborting the whole run.

**Already-exists is not a failure.** A uniqueness error means "already migrated". Resolve the
existing id and report it as such, so a re-run is safe.

**There is no upsert for most entities.** A loader must read before it writes, keyed on the
caller-owned source key.

**Generated GraphQL code is not committed.** `StrawberryShake.Server` writes it into `obj/` at
build time. Never hand-edit or commit generated output.

**Schema snapshots are committed and are not raw introspection.** Refresh one only through the
`DownloadSchema.sh` beside it.

## Verifying a change

Unit tests prove the mapping; only a real call proves the sample. Run it against a CoverGo
tenant before opening a pull request, run it a second time to prove the re-run is safe, and say
in the description what you ran and what came back.

## Style

Match the surrounding code. Comments explain *why* — a constraint, a trade-off, a trap — never
what the line already says. Do not add a comment that restates the code.
