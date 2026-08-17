using System.Runtime.CompilerServices;
using CoverGo.Samples.Domain.Agents;

namespace CoverGo.Samples.Infrastructure.Agents;

/// <summary>
/// Builds agents in code, with no file involved. The minimal reference: one record,
/// one API call, nothing to parse.
/// </summary>
/// <remarks>
/// Deliberately distinct from every record in the JSON extract: agent email, agentNumber
/// and registrationNumber are all uniqueness-checked, so two sample sources sharing any of
/// them would collide on whichever ran second.
/// </remarks>
public sealed class InCodeAgentSource : IAgentSource
{
    public async IAsyncEnumerable<Agent> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        yield return new Agent
        {
            PartyId = "SRC-AGT-INCODE-001",
            AgentNumber = "AGT-INCODE-001",
            // DistributorId is left unset so the sample runs against a tenant with no broker
            // structure yet. Set it to an id returned by creating a distributor to attach the
            // agent to a brokerage, which is what a real migration does.
            FirstName = "Devon",
            Surname = "Mercer",
            Email = "devon.mercer@brokerage.example",
            ActiveFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            // CountryCode is deliberately unset. CoverGo validates it against the CRY
            // reference table, so a tenant without that table configured rejects the
            // create outright. Set it once you have confirmed CRY is populated.
        };

        await Task.CompletedTask;
    }
}
