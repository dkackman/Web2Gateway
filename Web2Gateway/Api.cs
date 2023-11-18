using Web2Gateway;

internal static class Api
{
    public static WebApplication ConfigureApi(this WebApplication app, ILogger logger)
    {
        app.MapGet("/", () => Results.Redirect("/.well-known", true));
        app.MapGet("/.well-known", () =>
            {
                var g223 = app.Services.GetRequiredService<G2To3Service>();
                return g223.GetWellKnown();
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

        return app;
    }
}
