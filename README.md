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
| `CoverGo.Samples.Infrastructure.GatewayV1Client` | Generated client for the V1 gateway. Every operation the samples call. |
| `CoverGo.Samples.Infrastructure.GatewayV2Client` | Generated client for the V2 supergraph. |
| `CoverGo.Samples.Tests.Unit` | Unit tests, with xUnit and Moq. |

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
