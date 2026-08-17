using CoverGo.Samples.Application;
using CoverGo.Samples.Domain.Agents;
using CoverGo.Samples.Infrastructure.GatewayV1Client;
using StrawberryShake;

namespace CoverGo.Samples.Infrastructure.Agents;

/// <summary>
/// Creates agents one at a time through <c>createAgent</c> on the V1 gateway.
/// </summary>
/// <remarks>
/// This is the per-record shape: one API call per agent, by way of
/// <see cref="PerRecordTarget{T}"/>. CoverGo also offers a batch shape — <c>importAgents</c>
/// takes a whole extract and reports row-level results — which belongs behind the same
/// <see cref="IMigrationTarget{T}"/> port, so switching to it changes nothing else in the
/// program. Only the per-record shape is implemented here.
/// <para>
/// Two behaviours worth copying. The read before the write: CoverGo has no upsert for agents,
/// so the agent is looked up by its caller-owned <c>partyId</c> and skipped if present, which
/// makes a re-run safe. And <c>errors</c> is inspected even on HTTP 200 — ChannelManagement
/// returns validation failures inside the payload, not as top-level GraphQL errors.
/// </para>
/// </remarks>
public sealed class GatewayAgentTarget(IGatewayV1Client client) : PerRecordTarget<Agent>
{
    protected override async Task<LoadResult> LoadOneAsync(
        Agent record, CancellationToken cancellationToken)
    {
        IOperationResult<IAgentByPartyIdResult> lookup =
            await client.AgentByPartyId.ExecuteAsync(record.PartyId, cancellationToken);

        if (Describe(lookup) is { } lookupError)
        {
            // Without a usable read-back this record's idempotency cannot be decided, so fail it
            // rather than risk creating a duplicate.
            return LoadResult.Failed(record.PartyId, $"lookup failed: {lookupError}");
        }

        string? existing = lookup.Data?.Agents?.Items?.FirstOrDefault()?.AgentID;
        if (existing is not null)
        {
            return LoadResult.AlreadyExisted(record.PartyId, existing);
        }

        IOperationResult<ICreateAgentResult> result = await client.CreateAgent.ExecuteAsync(
            new CreateAgentInput
            {
                DistributorID = record.DistributorId,
                PartyId = record.PartyId,
                AgentNumber = record.AgentNumber,
                RegistrationNumber = record.RegistrationNumber,
                RegistrationDateTime = record.RegisteredOn,
                LicenseExpiryDateTime = record.LicenceExpiresOn,
                ActiveFromDateTime = record.ActiveFrom,
                Party = new IndividualPartyFieldsInput
                {
                    FirstName = record.FirstName,
                    Surname = record.Surname,
                    MainContactEmail = record.Email,
                    MainContactPhone = record.Phone,
                    CountryCode = record.CountryCode,
                },
            },
            cancellationToken);

        // Errors on a per-record call are attributed to that record, never thrown: one bad row
        // must not abandon the rest of the migration. Not every rejection arrives in the payload
        // -- a bad countryCode, for instance, comes back as a top-level GraphQL error -- so both
        // places have to be checked. A systemic fault such as a failed authorization shows up as
        // every record failing with the same message, which the report makes obvious.
        if (Describe(result) is { } callError)
        {
            return LoadResult.Failed(record.PartyId, callError);
        }

        ICreateAgent_CreateAgent? payload = result.Data?.CreateAgent;
        if (payload is null)
        {
            return LoadResult.Failed(record.PartyId, "createAgent returned no payload.");
        }

        if (payload.Errors is { Count: > 0 })
        {
            string? alreadyMigrated = AlreadyExists(payload.Errors);
            if (alreadyMigrated is not null)
            {
                string? found = await FindByPartyIdAsync(record.PartyId, cancellationToken);

                return found is not null
                    ? LoadResult.AlreadyExisted(record.PartyId, found)
                    // The clash is on some other unique field, so this row needs a human.
                    : LoadResult.Failed(record.PartyId,
                        $"{alreadyMigrated}, but no agent has this partyId — the clash is on "
                        + "agentNumber, registrationNumber or email.");
            }

            return LoadResult.Failed(record.PartyId,
                string.Join("; ", payload.Errors.Select(Describe)));
        }

        string? agentId = payload.AgentState?.AgentID;

        return agentId is not null
            ? LoadResult.Created(record.PartyId, agentId)
            : LoadResult.Failed(record.PartyId, "createAgent succeeded but returned no agentID.");
    }

    private async Task<string?> FindByPartyIdAsync(string partyId, CancellationToken cancellationToken)
    {
        IOperationResult<IAgentByPartyIdResult> result =
            await client.AgentByPartyId.ExecuteAsync(partyId, cancellationToken);

        return Describe(result) is null
            ? result.Data?.Agents?.Items?.FirstOrDefault()?.AgentID
            : null;
    }

    /// <summary>The call's errors as one message, or null when it carried none.</summary>
    private static string? Describe<TResult>(IOperationResult<TResult> result)
        where TResult : class
        => result.Errors.Count == 0
            ? null
            : string.Join("; ", result.Errors.Select(e => e.Message));

    /// <summary>The uniqueness guards mean "already migrated", so they are not failures.</summary>
    private static string? AlreadyExists(IReadOnlyList<ICreateAgent_CreateAgent_Errors> errors)
        => errors
            .Select(e => e switch
            {
                ICreateAgent_CreateAgent_Errors_AgentNumberAlreadyExistsError => "AgentNumberAlreadyExists",
                ICreateAgent_CreateAgent_Errors_RegistrationNumberAlreadyExistsError => "RegistrationNumberAlreadyExists",
                ICreateAgent_CreateAgent_Errors_EmailAlreadyExistsError => "EmailAlreadyExists",
                _ => null,
            })
            .FirstOrDefault(name => name is not null);

    private static string Describe(ICreateAgent_CreateAgent_Errors error)
        => error switch
        {
            ICreateAgent_CreateAgent_Errors_AgentFieldsValidationError fields =>
                $"{fields.Code}: {fields.Message} ("
                + string.Join(", ", (fields.FieldErrors ?? []).Select(f => $"{f?.Path}={f?.Message}"))
                + ")",
            _ => error.GetType().Name,
        };
}
