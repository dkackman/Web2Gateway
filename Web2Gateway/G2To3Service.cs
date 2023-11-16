using System.Dynamic;

namespace Web2Gateway;

public sealed class G2To3Service
{
    private readonly ChiaService _chiaService;
    private readonly ILogger<G2To3Service> _logger;
    private readonly IConfiguration _configuration;

    public G2To3Service(ChiaService chiaService, ILogger<G2To3Service> logger, IConfiguration configuration) =>
            (_chiaService, _logger, _configuration) = (chiaService, logger, configuration);

    public dynamic GetWellKnown()
    {
        dynamic wellKnown = new ExpandoObject();
        wellKnown.xch_address = _configuration.GetValue("App:xch_address", ""); ;
        wellKnown.donation_address = _configuration.GetValue("App:donation_address", "");
        return wellKnown;
    }
}