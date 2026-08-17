namespace CoverGo.Samples.Application;

/// <summary>
/// What a migration run did, per record.
/// </summary>
/// <remarks>
/// A run is not all-or-nothing: rejected records are reported and the rest still load, so the
/// caller decides what an acceptable failure count is. Reconcile a failure by its
/// <see cref="LoadResult.SourceKey"/>, fix the extract, and re-run — records that already
/// loaded come back as <see cref="LoadOutcome.AlreadyExisted"/>.
/// </remarks>
public sealed record MigrationReport
{
    public required IReadOnlyList<LoadResult> Results { get; init; }

    public int Created => Results.Count(r => r.Outcome is LoadOutcome.Created);

    public int AlreadyExisted => Results.Count(r => r.Outcome is LoadOutcome.AlreadyExisted);

    public int Failed => Results.Count(r => r.Outcome is LoadOutcome.Failed);

    public bool AnyFailed => Failed > 0;

    public IEnumerable<LoadResult> Failures => Results.Where(r => r.Outcome is LoadOutcome.Failed);
}
