using Microsoft.Extensions.Hosting;
using SUMMS.Api.Services.Interfaces;
using System.Threading;

namespace SUMMS.Api.Services;

public class CleanupHostedService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;

    public CleanupHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(() => CleanupAsync());
        
        _timer = new Timer(CleanupAsync, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

        return Task.CompletedTask;
    }

    private async void CleanupAsync(object? state = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
        await reservationService.CleanupExpiredReservationsAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
