using System.Dynamic;

namespace Web2Gateway;


public record WellKnown()
{
    public string xch_address { get; init; } = "";
    public string donation_address { get; init; } = "";
}

public sealed class G2To3Service
{
    private readonly ChiaService _chiaService;
    private readonly ILogger<G2To3Service> _logger;
    private readonly IConfiguration _configuration;

    public G2To3Service(ChiaService chiaService, ILogger<G2To3Service> logger, IConfiguration configuration) =>
            (_chiaService, _logger, _configuration) = (chiaService, logger, configuration);

    public WellKnown GetWellKnown()
    {
        return new WellKnown
        {
            xch_address = _configuration.GetValue("App:xch_address", "")!,
            donation_address = _configuration.GetValue("App:donation_address", "")!
        };
    }

    public async Task<IEnumerable<string>?> GetKeys(string storeId, CancellationToken cancellationToken)
    {
        var dataLayer = await _chiaService.GetDataLayer(cancellationToken) ?? throw new Exception("DataLayer not available");

        try
        {
            return await dataLayer.GetKeys(storeId, null, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
            return null;
        }
    }

    public async Task<string> GetValueAsHtml(string storeId, CancellationToken cancellationToken)
    {
        var hexKey = HexUtils.ToHex("index.html");
        var dataLayerResponse = await GetValue(storeId, hexKey, cancellationToken) ?? throw new InvalidOperationException("Couldn't retrieve expected key value");
        var value = HexUtils.FromHex(dataLayerResponse);
        // Add the base tag
        var baseTag = $"<base href=\"/{storeId}/\">";
        return value.Replace("<head>", $"<head>\n    {baseTag}");
    }

    public async Task<string?> GetValue(string storeId, string key, CancellationToken cancellationToken)
    {
        var dataLayer = await _chiaService.GetDataLayer(cancellationToken) ?? throw new Exception("DataLayer not available");

        try
        {
            return await dataLayer.GetValue(storeId, key, null, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
            return null;
        }
    }
}