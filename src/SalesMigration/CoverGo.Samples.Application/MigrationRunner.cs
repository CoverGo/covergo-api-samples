using CoverGo.Samples.Domain;
using Microsoft.Extensions.Logging;

namespace CoverGo.Samples.Application;

/// <summary>
/// Reads every record a source supplies, loads it into CoverGo through the target, and reports
/// the outcome of each one.
/// </summary>
/// <remarks>
/// The runner knows nothing about how the target talks to CoverGo — one call per record or one
/// call per batch — beyond <see cref="IMigrationTarget{T}.BatchSize"/>, which tells it how much
/// to hand over at a time. Records are streamed and chunked, so a large extract never has to
/// fit in memory.
/// </remarks>
public sealed class MigrationRunner<T>(
    IMigrationSource<T> source,
    IMigrationTarget<T> target,
    ILogger<MigrationRunner<T>> logger)
{
    public async Task<MigrationReport> RunAsync(CancellationToken cancellationToken = default)
    {
        int batchSize = Math.Max(1, target.BatchSize);
        List<LoadResult> all = [];
        List<T> batch = new(batchSize);

        await foreach (T record in source.ReadAsync(cancellationToken))
        {
            batch.Add(record);

            if (batch.Count == batchSize)
            {
                all.AddRange(await LoadAsync(batch, cancellationToken));
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            all.AddRange(await LoadAsync(batch, cancellationToken));
        }

        MigrationReport report = new() { Results = all };

        logger.LogInformation(
            "{EntityType}: {Created} created, {Existed} already present, {Failed} failed",
            typeof(T).Name, report.Created, report.AlreadyExisted, report.Failed);

        return report;
    }

    private async Task<IReadOnlyList<LoadResult>> LoadAsync(
        IReadOnlyList<T> batch, CancellationToken cancellationToken)
    {
        IReadOnlyList<LoadResult> results = await target.LoadAsync(batch, cancellationToken);

        foreach (LoadResult result in results)
        {
            // Source keys and identifiers only. Records carry personal data and are never logged.
            switch (result.Outcome)
            {
                case LoadOutcome.Created:
                    logger.LogInformation("Created {EntityType} {Id} from {SourceKey}",
                        typeof(T).Name, result.Id, result.SourceKey);
                    break;

                case LoadOutcome.AlreadyExisted:
                    logger.LogInformation("{SourceKey} already loaded as {Id}; skipped",
                        result.SourceKey, result.Id);
                    break;

                case LoadOutcome.Failed:
                    logger.LogError("{SourceKey} rejected: {Error}", result.SourceKey, result.Error);
                    break;
            }
        }

        return results;
    }
}
