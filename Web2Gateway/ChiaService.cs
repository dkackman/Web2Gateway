using chia.dotnet;

namespace Web2Gateway;

/// <summary>
/// Provides methods for interacting with the Chia blockchain.
/// </summary>
public sealed class ChiaService
{
    private readonly HttpRpcClient _rpcClient;
    private readonly ILogger<ChiaService> _logger;
    private readonly IConfiguration _configuration;

    public ChiaService(HttpRpcClient rpcClient, ILogger<ChiaService> logger, IConfiguration configuration) =>
            (_rpcClient, _logger, _configuration) = (rpcClient, logger, configuration);

    public DataLayerProxy? GetDataLayer(CancellationToken stoppingToken)
    {
        try
        {
            return new DataLayerProxy(_rpcClient, "DlMirrorSync");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
            return null;
        }
    }
}
