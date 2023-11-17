using chia.dotnet;

namespace Web2Gateway;

/// <summary>
/// Provides methods for interacting with the Chia blockchain.
/// </summary>
public sealed class ChiaService
{
    private readonly ILogger<ChiaService> _logger;
    private readonly IConfiguration _configuration;

    public ChiaService(ILogger<ChiaService> logger, IConfiguration configuration) =>
            (_logger, _configuration) = (logger, configuration);

    private Config GetConfig()
    {
        var configPath = _configuration.GetValue("App:chia_config_path", "");
        if (!string.IsNullOrEmpty(configPath))
        {
            _logger.LogInformation("Using config file at {Path}", configPath);
            return Config.Open(configPath);
        }

        return Config.Open();
    }

    public async Task<DataLayerProxy?> GetDataLayer(CancellationToken stoppingToken)
    {
        try
        {
            var endpoint = GetConfig().GetEndpoint("data_layer");

            _logger.LogInformation("Connecting to data layer at {Uri}", endpoint.Uri);
            var rpcClient = new HttpRpcClient(endpoint);

            var dl = new DataLayerProxy(rpcClient, "DlMirrorSync");
            await dl.HealthZ(stoppingToken);
            return dl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
            return null;
        }
    }
}