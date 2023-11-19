using chia.dotnet;

namespace Web2Gateway;

/// <summary>
/// Provides methods for interacting with the Chia blockchain.
/// </summary>
public sealed class ChiaService
{
    private readonly object _syncLock = new();
    private EndpointInfo? _endpointInfo = null!;
    private readonly ILogger<ChiaService> _logger;
    private readonly IConfiguration _configuration;

    public ChiaService(ILogger<ChiaService> logger, IConfiguration configuration) =>
            (_logger, _configuration) = (logger, configuration);

    private Config GetConfig()
    {
        // first see if we have a config file path in the appsettings.json
        var configPath = _configuration.GetValue("App:chia_config_path", "");
        if (!string.IsNullOrEmpty(configPath))
        {
            _logger.LogInformation("Using config file at {Path}", configPath);
            return Config.Open(configPath);
        }

        // if not use the chia default '~/.chia/mainnet/config/config.yaml'
        return Config.Open();
    }

    private EndpointInfo GetDataLayerEndpoint()
    {
        lock (_syncLock)
        {
            if (_endpointInfo is not null)
            {
                return _endpointInfo;
            }

            // first check user secrets for the data_layer connection
            // https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0&tabs=windows
            var dataLayerUri = _configuration.GetValue("data_layer_uri", "")!;
            if (!string.IsNullOrEmpty(dataLayerUri))
            {
                _endpointInfo = new EndpointInfo()
                {
                    Uri = new Uri(dataLayerUri),
                    // when stored in an environment variable the newlines might be escaped
                    Cert = _configuration.GetValue("data_layer_cert", "")!.Replace("\\n", "\n"),
                    Key = _configuration.GetValue("data_layer_key", "")!.Replace("\\n", "\n")
                };
            }
            else
            {
                // if not present see if we can get it from the config file
                _endpointInfo = GetConfig().GetEndpoint("data_layer");
            }

            return _endpointInfo;
        }
    }

    public DataLayerProxy? GetDataLayer(CancellationToken stoppingToken)
    {
        try
        {
            var endpoint = GetDataLayerEndpoint();

            _logger.LogInformation("Connecting to data layer at {Uri}", endpoint.Uri);
            var rpcClient = new HttpRpcClient(endpoint);

            return new DataLayerProxy(rpcClient, "DlMirrorSync");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
            return null;
        }
    }
}
