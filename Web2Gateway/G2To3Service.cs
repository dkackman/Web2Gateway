using System.Dynamic;

namespace Web2Gateway;

public sealed class G2To3Service
{
    private readonly ChiaService _chiaService;
    private readonly ILogger<G2To3Service> _logger;
    private readonly IConfiguration _configuration;

    public G2To3Service(ChiaService chiaService, ILogger<G2To3Service> logger, IConfiguration configuration) =>
            (_chiaService, _logger, _configuration) = (chiaService, logger, configuration);

    public async Task<dynamic> GetWellKnown(CancellationToken cancellationToken)
    {
        var xchWalletId = _configuration.GetValue<uint>("App:xch_wallet_id", 1);
        var wallet = await _chiaService.GetWallet(xchWalletId, cancellationToken) ?? throw new Exception("Wallet not found");
        var address = await wallet.GetNextAddress(newAddress: _configuration.GetValue("App:new_address", false), cancellationToken);

        dynamic wellKnown = new ExpandoObject();
        wellKnown.xch_address = address;
        wellKnown.donation_address = _configuration.GetValue("App:donation_address", "blank");
        return wellKnown;
    }
}