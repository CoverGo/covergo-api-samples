namespace CoverGo.Samples.Domain;

/// <summary>
/// Supplies the records of one entity type to a sample.
/// </summary>
/// <remarks>
/// This is the seam between CoverGo's code and the customer's. CoverGo ships two
/// reference implementations per entity — one that parses a JSON extract, one that
/// builds the record in code — and the customer replaces them with an implementation
/// that reads their own extract. Nothing else in the samples needs to change.
/// </remarks>
public interface IMigrationSource<out T>
{
    /// <summary>Streams the records to load, so a large extract is never held in memory at once.</summary>
    IAsyncEnumerable<T> ReadAsync(CancellationToken cancellationToken = default);
}
