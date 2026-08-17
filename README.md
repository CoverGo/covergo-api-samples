# CoverGo API Samples

Reference C# programs showing how to call the CoverGo APIs.

> These samples are reference material, not a supported SDK. They carry no service-level
> commitment. Copy what you need into your own codebase.

## Status

This repository is being populated. The first sample covers migrating a sales structure —
distributors and agents — into a CoverGo tenant; clients, policies and documents follow.

## You supply the data, the samples supply the API calls

Every sample reads its input through an interface. CoverGo ships two reference implementations
of each one — a JSON extract reader, and a version that builds the record in code — and you
replace them with an implementation that reads your own extract. Nothing else has to change.

That seam is `IMigrationSource<T>` in the Domain layer. Domain and Application hold no
dependency on the generated GraphQL clients, so your mapping code never has to know that
CoverGo speaks GraphQL.

Two load shapes sit behind one port, `IMigrationTarget<T>`: one call per record, or one call
for a whole batch. Which one a sample uses is invisible to the rest of the program.

## Layout

Everything for one migration lives under a single folder, `src/SalesMigration`:

| Project | Holds |
| --- | --- |
| `CoverGo.Samples.Domain` | `IMigrationSource<T>` — the contract you implement. No dependencies. |
| `CoverGo.Samples.Application` | Use cases: `MigrationRunner<T>`, the `IMigrationTarget<T>` port, and the per-record result and report types. Depends on Domain only. |
| `CoverGo.Samples.Infrastructure` | Configuration, authentication and mapping. The only layer that knows CoverGo speaks GraphQL. |
| `CoverGo.Samples.Infrastructure.GatewayV1Client` | Generated client for the V1 gateway. Every operation the samples call. |
| `CoverGo.Samples.Infrastructure.GatewayV2Client` | Generated client for the V2 supergraph. |
| `CoverGo.Samples.AgentMigrationApp` | Runnable console host for the agent sample. |
| `CoverGo.Samples.Tests.Unit` | Unit tests, with xUnit and Moq. |

## The first sample: agents

`CoverGo.Samples.AgentMigrationApp` loads agents into a tenant. It shows the two things a
migration actually has to get right.

**Parsing and mapping are separate steps.** `JsonFileAgentSource` parses an extract into the
`Agent` record with `System.Text.Json`; `GatewayAgentTarget` maps that record onto the
`createAgent` input. Your own source implementation replaces the first and leaves the second
alone. `InCodeAgentSource` builds the same record by hand, for when you want one call and no
file.

**A re-run must not duplicate.** CoverGo has no upsert for agents, so the target reads by your
own `partyId` before it writes and reports the record as already-migrated instead of creating a
second one. Uniqueness errors coming back from CoverGo are treated the same way rather than as
failures. Run it twice — the second run should create nothing.

The run ends with a report counting created, already-existed and failed records, and exits
non-zero if any record failed. A single bad row never abandons the rest of the extract.

```bash
dotnet run --project src/SalesMigration/CoverGo.Samples.AgentMigrationApp -- \
  src/SalesMigration/data/agents.sample.json
```

## Configuration

Settings bind from `appsettings.json`, then environment variables, then user secrets — each
overriding the last. A missing setting stops the program at startup and names it, rather than
surfacing later as an authentication failure.

`appsettings.json` ships **placeholder values only**, and carries no client secret at all.
Nothing in this repository is a working credential, and nothing that is one should ever be
committed here. Supply the real values at run time:

```bash
# Locally
dotnet user-secrets set "CoverGo:ClientId"     "<your-client-id>"
dotnet user-secrets set "CoverGo:ClientSecret" "<your-client-secret>"

# Anywhere else
export CoverGo__ClientId="<your-client-id>"
export CoverGo__ClientSecret="<your-client-secret>"
```

CoverGo provisions your client application and tells you its id, plus the tenant and gateway
URLs to use.

## Authentication

A `DelegatingHandler` acquires an OAuth2 `client_credentials` token, caches it in memory and
attaches it to every gateway request. One token serves a whole run: it is refreshed only when
it is close to expiry, so a batch of a thousand records still calls the token endpoint once.

## Building and testing

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) and Node 24 or newer.

```bash
npm ci            # installs Nx and the .NET plugin
npm run build     # nx run-many -t build
npm run lint      # nx run-many -t lint   (dotnet format --verify-no-changes)
npm run test      # nx run-many -t test
```

Projects and targets are inferred by [`@nx-dotnet/core`](https://github.com/nx-dotnet/nx-dotnet)
directly from the `.csproj` files, including the dependency graph, so adding a project needs no
Nx configuration. Nx caches per project, so `npm run affected:build` rebuilds only what a change
touched — which is what CI runs on a pull request.

Warnings are errors here. That is deliberate: it is cheaper to fix a nullability warning than to
debug the null it predicted.

## Regenerating a GraphQL schema

Each client project holds a committed `schema.graphql` snapshot and the `DownloadSchema.sh`
that produced it. Generated C# is **not** committed — Strawberry Shake writes it into `obj/`
at build time from the snapshot and the operation documents in `Queries/` and `Mutations/`.

Refresh a snapshot with the script beside it rather than by hand, and review the diff. The V1
script also runs a strip step, which is required rather than cosmetic: the raw V1 introspection
declares interfaces with no implementing type, and Strawberry Shake refuses to generate from
that.

## Documentation

The API reference these samples implement — authentication, gateways, per-entity operations,
error shapes and idempotency — is published in Confluence. Ask your CoverGo contact for
access.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) first. By participating you agree to the
[Code of Conduct](CODE_OF_CONDUCT.md). To report a vulnerability, follow
[SECURITY.md](SECURITY.md) rather than opening an issue.

## Licence

Apache License 2.0 — see [LICENSE](LICENSE) and [NOTICE](NOTICE).
