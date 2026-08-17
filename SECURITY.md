# Security Policy

## Reporting a vulnerability

**Do not open a public issue.** Use GitHub's private vulnerability reporting on this
repository — the **Security** tab, then **Report a vulnerability**. That opens a channel
visible only to the maintainers.

If you cannot use that channel, contact your CoverGo representative directly and ask to reach
the security team. Please do not post details in a pull request, issue, or discussion.

Include what you found, how to reproduce it, and the impact you believe it has. We will
acknowledge the report and tell you what we intend to do about it.

## Scope

This repository holds **reference sample code**, not a running service. A finding here is
usually one of:

- a committed credential, token, or piece of customer data;
- a sample that teaches an unsafe pattern — logging personal data, disabling certificate
  validation, storing a secret on disk;
- a dependency with a known vulnerability that reaches the samples.

Vulnerabilities in the CoverGo platform itself are **out of scope for this repository**.
Report those through your CoverGo representative so they reach the platform security team.

## Credentials

Nothing in this repository is a working credential. `appsettings.json` carries placeholder
values only; real client ids and secrets are supplied at run time through `dotnet user-secrets`
or environment variables. Secret scanning and push protection are enabled.

If you believe a real credential has been committed here, treat it as compromised and report
it through the channel above so it can be rotated. Deleting the file in a later commit does
not remove it from history.
