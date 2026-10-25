using Election.Infrastructure;
using Election.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net;

namespace Election.Tests;

public class WarmupTests
{
    [Fact]
    public async Task ColdApiRequestsReceiveSnapshotWhenExistingWorkerFinishes()
    {
        await using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                foreach (var worker in services.Where(d => d.ServiceType == typeof(IHostedService)).ToArray())
                    services.Remove(worker);
            }));
        using var client = app.CreateClient();
        var warmup = app.Services.GetRequiredService<ElectionWarmup>();
        warmup.Begin();
        var brazil = client.GetAsync("/api/election?round=1");
        var states = client.GetAsync("/api/election/states?round=1");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/election?round=3")).StatusCode);
        Assert.False(brazil.IsCompleted);
        Assert.False(states.IsCompleted);
        var cache = app.Services.GetRequiredService<IElectionCache>();
        cache.Store(await new MockElectionProvider(TimeProvider.System, Options.Create(new ElectionOptions()))
            .FetchAsync(default, 1));
        warmup.Complete();
        Assert.Equal(HttpStatusCode.OK, (await brazil).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await states).StatusCode);
    }

    [Fact]
    public async Task SecondRoundRequestsWaitForSecondRoundEvenWhenFirstRoundIsCached()
    {
        await using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                foreach (var worker in services.Where(d => d.ServiceType == typeof(IHostedService)).ToArray())
                    services.Remove(worker);
            }));
        using var client = app.CreateClient();
        var warmup = app.Services.GetRequiredService<ElectionWarmup>();
        var cache = app.Services.GetRequiredService<IElectionCache>();
        var mock = new MockElectionProvider(TimeProvider.System, Options.Create(new ElectionOptions()));
        warmup.Begin();
        cache.Store(await mock.FetchAsync(default, 1));
        var brazil = client.GetAsync("/api/election?round=2");
        await Task.Delay(200);
        Assert.False(brazil.IsCompleted);
        cache.Store(await mock.FetchAsync(default, 2));
        warmup.Complete();
        var response = await brazil;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("\"candidatesOnly\":true", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ConcurrentRequestsWaitForOneWorkerAndCancellationDoesNotCancelWarmup()
    {
        var warmup = new ElectionWarmup();
        warmup.Begin();
        using var cancelled = new CancellationTokenSource();
        var first = warmup.WaitAsync(TimeSpan.FromSeconds(5), cancelled.Token);
        var second = warmup.WaitAsync(TimeSpan.FromSeconds(5), default);
        Assert.False(second.IsCompleted);
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Assert.False(second.IsCompleted);
        warmup.Complete();
        await second;
        await warmup.WaitAsync(TimeSpan.Zero, default);
    }

    [Fact]
    public async Task SlowOrDisabledWorkerDoesNotBlockRequestsIndefinitely()
    {
        var warmup = new ElectionWarmup();
        await warmup.WaitAsync(TimeSpan.Zero, default);
        warmup.Begin();
        await warmup.WaitAsync(TimeSpan.Zero, default);
        warmup.Complete();
        await warmup.WaitAsync(TimeSpan.Zero, default);
    }
}
