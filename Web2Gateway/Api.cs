using Web2Gateway;
using System.Text.RegularExpressions;

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
                    var keys = await g223.GetKeys(storeId, cancellationToken);

                    if (keys is not null)
                    {
                        var decodedKeys = keys.Select(key => HexUtils.FromHex(key)).ToList();

                        // the key represents a SPA app, so we want to return the index.html
                        if (decodedKeys != null && decodedKeys.Count > 0 && decodedKeys.Contains("index.html") && showKeys != true)
                        {
                            var html = await g223.GetValueAsHtml(storeId, cancellationToken);
                            return Results.Content(html, "text/html");
                        }

                        return Results.Ok(decodedKeys);
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

        app.MapGet("/{storeId}/{*catchAll}", async (HttpContext httpContext, string storeId, string catchAll, CancellationToken cancellationToken) =>
            {
                try
                {
                    var key = catchAll;
                    // Remove everything after the first '#'
                    if (key.Contains('#'))
                    {
                        key = key.Split('#')[0];
                    }
                    key = key.TrimEnd('/');

                    // A referrer indicates that the user is trying to access the store from a website
                    // we want to redirect them so that the URL includes the storeId in the path
                    var referer = httpContext.Request.Headers["referer"].ToString();
                    if (!string.IsNullOrEmpty(referer) && !referer.Contains(storeId))
                    {
                        if (key.StartsWith("/"))
                        {
                            key = key[1..];
                        }

                        httpContext.Response.Headers["Location"] = $"{referer}/{storeId}/{key}";
                        return Results.Redirect($"{referer}/{storeId}/{key}", true);
                    }

                    var hexKey = HexUtils.ToHex(key);
                    var g223 = app.Services.GetRequiredService<G2To3Service>();
                    var dataLayerResponse = await g223.GetValue(storeId, hexKey, cancellationToken);
                    if (dataLayerResponse is null)
                    {
                        return Results.NotFound();
                    }

                    var decodedValue = HexUtils.FromHex(dataLayerResponse);
                    var fileExtension = Path.GetExtension(decodedValue);

                    if (Utils.TryParseJson(decodedValue, out var json) && json?.type == "multipart")
                    {
                        string mimeType = Utils.GetMimeType(fileExtension) ?? "application/octet-stream";
                        var bytes = await g223.GetValuesAsBytes(storeId, json, cancellationToken);
                        httpContext.Response.ContentType = mimeType;
                        await httpContext.Response.Body.WriteAsync(bytes, cancellationToken);
                        await httpContext.Response.CompleteAsync();
                        return Results.StatusCode(StatusCodes.Status200OK);
                    }
                    else if (!string.IsNullOrEmpty(fileExtension))
                    {
                        string mimeType = Utils.GetMimeType(fileExtension) ?? "application/octet-stream";
                        return Results.Content(decodedValue, mimeType);
                    }
                    else if (json is not null)
                    {
                        return Results.Ok(json);
                    }
                    else if (Utils.IsBase64Image(decodedValue))
                    {
                        string base64Image = decodedValue.Split(";base64,")[^1];
                        byte[] imageBuffer = Convert.FromBase64String(base64Image);

                        var regex = new Regex(@"[^:]\w+\/[\w-+\d.]+(?=;|,)");
                        var match = regex.Match(decodedValue);
                        httpContext.Response.ContentType = match.Value;
                        await httpContext.Response.Body.WriteAsync(imageBuffer, cancellationToken);
                        await httpContext.Response.CompleteAsync();
                        return Results.StatusCode(StatusCodes.Status200OK);
                    }
                    else
                    {
                        return Results.Content(decodedValue);
                    }
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
        .WithName("{storeId}/*")
        .WithOpenApi();

        return app;
    }
}
