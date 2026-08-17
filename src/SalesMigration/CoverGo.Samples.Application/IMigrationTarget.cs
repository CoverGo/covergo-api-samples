namespace CoverGo.Samples.Application;

/// <summary>
/// Loads records into CoverGo.
/// </summary>
/// <remarks>
/// The interface takes a batch rather than a single record because CoverGo offers both shapes
/// and a migration should be able to use either without the rest of the program changing:
/// <list type="bullet">
///   <item>one call per record — <c>createAgent</c>, <c>createIndividual</c>, <c>createDistributor</c>;</item>
///   <item>one call for many records — <c>importAgents</c>, which takes a whole CSV and reports
///         row-level results.</item>
/// </list>
/// A per-record implementation should derive from <see cref="PerRecordTarget{T}"/> rather than
/// implement this directly.
/// </remarks>
public interface IMigrationTarget<in T>
{
    /// <summary>
    /// How many records one <see cref="LoadAsync"/> call should carry. The runner chunks the
    /// source to this size. A per-record target uses 1; a batch target uses whatever the bulk
    /// endpoint handles comfortably.
    /// </summary>
    int BatchSize { get; }

    /// <summary>
    /// Loads a batch and reports the outcome of every record in it, in any order.
    /// </summary>
    /// <remarks>
    /// A rejected record is reported as a failed <see cref="LoadResult"/>, not thrown: one bad
    /// row must not abandon the rest of the migration. Throw only when the whole call failed —
    /// a transport error, or authorization.
    /// </remarks>
    Task<IReadOnlyList<LoadResult>> LoadAsync(
        IReadOnlyList<T> batch, CancellationToken cancellationToken = default);
}
