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
        var dataLayer = _chiaService.GetDataLayer(cancellationToken) ?? throw new Exception("DataLayer not available");

        try
        {
            return await dataLayer.GetKeys(storeId, null, cancellationToken);
        }
        catch
        {
            return null;  // 404 in the api
        }
    }

    public async Task<string> GetValueAsHtml(string storeId, CancellationToken cancellationToken)
    {
        var hexKey = HexUtils.ToHex("index.html");
        var value = await GetValue(storeId, hexKey, cancellationToken) ?? throw new InvalidOperationException("Couldn't retrieve expected key value");
        var decodedValue = HexUtils.FromHex(value);
        var baseTag = $"<base href=\"/{storeId}/\">"; // Add the base tag

        return decodedValue.Replace("<head>", $"<head>\n    {baseTag}");
    }

    public async Task<byte[]> GetValuesAsBytes(string storeId, dynamic json, CancellationToken cancellationToken)
    {
        var multipartFileNames = json.parts as IEnumerable<string> ?? new List<string>();
        var sortedFileNames = new List<string>(multipartFileNames);
        sortedFileNames.Sort((a, b) =>
            {
                int numberA = int.Parse(a.Split(".part")[1]);
                int numberB = int.Parse(b.Split(".part")[1]);
                return numberA.CompareTo(numberB);
            });

        var hexPartsPromises = multipartFileNames.Select(async fileName =>
        {
            var hexKey = HexUtils.ToHex(fileName);
            return await GetValue(storeId, hexKey, cancellationToken);
        });
        var dataLayerResponses = await Task.WhenAll(hexPartsPromises);
        var resultHex = string.Join("", dataLayerResponses);

        return Convert.FromHexString(resultHex);
    }

    public async Task<string?> GetValue(string storeId, string key, CancellationToken cancellationToken)
    {
        var dataLayer = _chiaService.GetDataLayer(cancellationToken) ?? throw new Exception("DataLayer not available");

        try
        {
            return await dataLayer.GetValue(storeId, key, null, cancellationToken);
        }
        catch
        {
            return null; // 404 in the api
        }
    }
}
