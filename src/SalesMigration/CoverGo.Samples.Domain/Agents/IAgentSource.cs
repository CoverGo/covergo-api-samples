namespace CoverGo.Samples.Domain.Agents;

/// <summary>
/// Supplies the agents to load. Implement this against your own extract; the two
/// implementations CoverGo ships are references, not the intended production path.
/// </summary>
public interface IAgentSource : IMigrationSource<Agent>;
