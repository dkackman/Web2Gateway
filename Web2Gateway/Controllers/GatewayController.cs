using Microsoft.AspNetCore.Mvc;

namespace Web2Gateway.Controllers;

[ApiController]
[Route("[controller]")]
public class GatewayController : ControllerBase
{
    private readonly ILogger<GatewayController> _logger;

    public GatewayController(ILogger<GatewayController> logger) => (_logger) = (logger);

    [HttpGet(Name = "{storeId}")]
    public string GetStoreId()
    {
        return "hi";
    }

    [HttpGet(Name = "GetWellKnown")]
    public string GetWellKnown()
    {
        return "hi";
    }
}
