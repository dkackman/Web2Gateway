using Web2Gateway;

// doing all of this in the mini-api expressjs-like approach

var builder = WebApplication.CreateBuilder(args);

// when run as a service, we need an explicit path to a chia config
// file, which can be set in a appsettings.json file and passed on the command line
if (args.Any())
{
    var configurationBinder = new ConfigurationBuilder()
        .AddJsonFile(args.First());

    var config = configurationBinder.Build();
    builder.Configuration.AddConfiguration(config);
}

// Add services to the container.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ChiaService>();
builder.Services.AddSingleton<G2To3Service>();

var logger = LoggerFactory.Create(config =>
{
    config.AddConsole();
}).CreateLogger("Program");
var configuration = builder.Configuration;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/.well-known", () =>
    {
        try
        {
            var g223 = app.Services.GetRequiredService<G2To3Service>();
            return g223.GetWellKnown();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}", ex.Message);
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    })
.WithName(".well-known")
.WithOpenApi();

app.MapGet("/{storeId}", async (HttpContext httpContext, string storeId, bool? showKeys, CancellationToken cancellationToken) =>
    {
        try
        {
            storeId = storeId.TrimEnd('/');

            // A referrer indicates that the user is trying to access the store from a website
            // we want to redirect them so that the URL includes the storeId in the path

            var referer = httpContext.Request.Headers["referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && referer.Contains(storeId))
            {
                httpContext.Response.Headers["Location"] = $"{referer}/{storeId}";
                return Results.Redirect($"{referer}/{storeId}", true);
            }

            var g223 = app.Services.GetRequiredService<G2To3Service>();
            var keys = await g223.GetKeys(storeId, cancellationToken) as IEnumerable<string>;

            if (keys is not null)
            {
                var apiResponse = keys.Select(key => HexUtils.FromHex(key)).ToList();

                if (apiResponse != null && apiResponse.Count > 0 && apiResponse.Contains("index.html") && showKeys != true)
                {
                    var html = await g223.GetValueAsHtml(storeId, cancellationToken);

                    // Set Content-Type to HTML and send the decoded value
                    httpContext.Response.ContentType = "text/html";
                    await httpContext.Response.WriteAsync(html, cancellationToken);
                    return Results.Ok();
                }

                return Results.Ok(apiResponse);
            }

            return Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "{Message}", ex.Message);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}", ex.Message);
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    })
.WithName("{storeId}")
.WithOpenApi();

app.UseCors()
    .UseHttpsRedirection()
    .UseAuthorization();
app.MapControllers();
app.Run();
