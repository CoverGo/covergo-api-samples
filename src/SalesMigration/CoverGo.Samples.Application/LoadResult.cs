namespace CoverGo.Samples.Application;

public enum LoadOutcome
{
    /// <summary>The record did not exist and CoverGo created it.</summary>
    Created,

    /// <summary>The record was already in CoverGo. A re-run reports this, not a failure.</summary>
    AlreadyExisted,

    /// <summary>CoverGo rejected this record. The rest of the batch is unaffected.</summary>
    Failed,
}

/// <summary>
/// The outcome for one source record.
/// </summary>
/// <remarks>
/// Per-record rather than per-call on purpose: a batch API reports row-level outcomes and can
/// partially succeed, so one call can produce a mix of created, already-existing and failed.
/// <see cref="SourceKey"/> is the caller's own key, which is what lets a failed row be found
/// in the extract and retried.
/// </remarks>
public sealed record LoadResult
{
    public required string SourceKey { get; init; }

    public required LoadOutcome Outcome { get; init; }

    /// <summary>The identifier CoverGo assigned. Null only when the load failed.</summary>
    public string? Id { get; init; }

    /// <summary>Why it failed, in the words CoverGo used. Null unless the load failed.</summary>
    public string? Error { get; init; }

    public static LoadResult Created(string sourceKey, string id) =>
        new() { SourceKey = sourceKey, Outcome = LoadOutcome.Created, Id = id };

    public static LoadResult AlreadyExisted(string sourceKey, string id) =>
        new() { SourceKey = sourceKey, Outcome = LoadOutcome.AlreadyExisted, Id = id };

    public static LoadResult Failed(string sourceKey, string error) =>
        new() { SourceKey = sourceKey, Outcome = LoadOutcome.Failed, Error = error };
}
