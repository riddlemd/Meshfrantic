namespace Meshfrantic.Services;

public class MeshtasticReaderService : BackgroundService
{
    private readonly MeshtasticService _meshtastic;
    private readonly ILogger<MeshtasticReaderService> _logger;

    public MeshtasticReaderService(MeshtasticService meshtastic, ILogger<MeshtasticReaderService> logger)
    {
        _meshtastic = meshtastic;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The read loop is started by MeshtasticService.ConnectAsync(), not here.
        // This service just keeps alive until the app shuts down.
        _logger.LogInformation("MeshtasticReaderService started");
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
    }
}
