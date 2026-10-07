namespace Election.Infrastructure;

// Requests may await the worker's first attempt; they never start upstream collections.
public sealed class ElectionWarmup
{
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int started;

    public void Begin() => Interlocked.Exchange(ref started, 1);
    public void Complete() => ready.TrySetResult();

    public async Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref started) == 0) return;
        try { await ready.Task.WaitAsync(timeout, cancellationToken); }
        catch (TimeoutException) { /* Keep the existing 503 contract if warmup is slow. */ }
    }
}
