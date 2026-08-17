namespace CoverGo.Samples.Domain.Agents;

/// <summary>
/// One individual broker to load into CoverGo, in the shape the customer's extract
/// provides it. Mapped onto the CoverGo <c>createAgent</c> input by the Infrastructure layer.
/// </summary>
/// <remarks>
/// CoverGo has no <c>broker</c> entity. A broker is three nested entities — sales channel,
/// distributor (the brokerage firm), agent (the individual) — so an agent is always created
/// against a <see cref="DistributorId"/> obtained from the distributor sample.
/// </remarks>
public sealed record Agent
{
    /// <summary>
    /// The customer's own key for this broker, stored verbatim on CoverGo's <c>partyId</c>.
    /// This is the reconciliation key: it is caller-owned, never server-generated, and
    /// filterable on read-back, so a re-run looks the agent up by it before creating.
    /// </summary>
    public required string PartyId { get; init; }

    /// <summary>Caller-supplied and uniqueness-checked. A repeat create reports it already exists.</summary>
    public required string AgentNumber { get; init; }

    /// <summary>
    /// The brokerage this agent belongs to — an id returned by creating a distributor.
    /// Nullable because CoverGo's schema allows an unattached agent, though a migration
    /// normally sets it. Leaving it null is useful in a tenant whose broker reference data
    /// is not configured yet.
    /// </summary>
    public string? DistributorId { get; init; }

    public required string FirstName { get; init; }

    public required string Surname { get; init; }

    /// <summary>Validated by CoverGo; a duplicate is rejected because the email identifies a person.</summary>
    public required string Email { get; init; }

    public required DateTimeOffset ActiveFrom { get; init; }

    public string? Phone { get; init; }

    /// <summary>
    /// ISO 3166-1 alpha-2, e.g. <c>CA</c>. Validated against the <c>CRY</c> reference table:
    /// a tenant without that table configured rejects any value, so leave it null there.
    /// </summary>
    public string? CountryCode { get; init; }

    /// <summary>Uniqueness-checked when supplied.</summary>
    public string? RegistrationNumber { get; init; }

    public DateTimeOffset? RegisteredOn { get; init; }

    /// <summary>Must not precede <see cref="RegisteredOn"/>; CoverGo rejects the pair otherwise.</summary>
    public DateTimeOffset? LicenceExpiresOn { get; init; }
}
