using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoverGo.Samples.Domain.Agents;

namespace CoverGo.Samples.Infrastructure.Agents;

/// <summary>
/// Reads agents from a JSON extract. This is the shape a customer ETL usually takes:
/// export to a file, then load. See <c>data/agents.sample.json</c> for the format.
/// </summary>
/// <remarks>
/// The file is streamed rather than read whole, so a large extract does not have to fit
/// in memory. Replace this class with one that reads your own export — nothing else changes.
/// </remarks>
public sealed class JsonFileAgentSource(string path) : IAgentSource
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public async IAsyncEnumerable<Agent> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Agent extract not found at '{path}'.", path);
        }

        await using FileStream stream = File.OpenRead(path);

        IAsyncEnumerable<Agent?> agents = JsonSerializer.DeserializeAsyncEnumerable<Agent>(
            stream, Options, cancellationToken);

        await foreach (Agent? agent in agents)
        {
            // A null element means a literal `null` in the JSON array; skip rather than fail the run.
            if (agent is not null)
            {
                yield return agent;
            }
        }
    }
}
