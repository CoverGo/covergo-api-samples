# CoverGo API Samples

Reference C# programs showing how to call the CoverGo APIs.

> These samples are reference material, not a supported SDK. They carry no service-level
> commitment. Copy what you need into your own codebase.

## Status

This repository is being populated. The first sample covers migrating a sales structure —
distributors and agents — into a CoverGo tenant; policies, clients and documents follow.

## What is here

Each sample is a runnable console program that authenticates with the OAuth2
`client_credentials` grant and calls one CoverGo API. You supply the data through an
interface; the sample supplies the mapping and the call. Nothing in this repository is a
working credential, and nothing that is one should ever be committed here.

Once a sample lands, its folder carries its own README with the operations it calls and the
constraints CoverGo enforces on them.

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) and Node 24 or newer.

```bash
npm ci        # installs Nx and the .NET plugin
npm run build
npm run test
```

Real credentials are supplied at run time through `dotnet user-secrets` or the environment —
never through a committed file. See [CONTRIBUTING.md](CONTRIBUTING.md).

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
