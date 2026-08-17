namespace CoverGo.Samples.Application;

/// <summary>
/// Base class for a target backed by an API that creates one record per call, which is most
/// of them. Implement <see cref="LoadOneAsync"/> and the batching is handled here.
/// </summary>
public abstract class PerRecordTarget<T> : IMigrationTarget<T>
{
    /// <summary>
    /// One record per call. The API is invoked once per record either way, so there is nothing
    /// to gain from a larger chunk.
    /// </summary>
    public virtual int BatchSize => 1;

    protected abstract Task<LoadResult> LoadOneAsync(T record, CancellationToken cancellationToken);

    public async Task<IReadOnlyList<LoadResult>> LoadAsync(
        IReadOnlyList<T> batch, CancellationToken cancellationToken = default)
    {
        List<LoadResult> results = new(batch.Count);

        foreach (T record in batch)
        {
            results.Add(await LoadOneAsync(record, cancellationToken));
        }

        return results;
    }
}
