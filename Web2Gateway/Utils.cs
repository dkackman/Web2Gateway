using System.Text.Json;

namespace Web2Gateway;

internal static class Utils
{
    public static bool IsBase64Image(string data)
    {
        return data.StartsWith("data:image", StringComparison.OrdinalIgnoreCase);
    }

    public static string? GetMimeType(string ext)
    {
        if (MimeTypes.TryGetMimeType(ext, out var mimeType))
        {
            return mimeType;
        }

        return null;
    }
    public static bool TryParseJson(string strInput, out dynamic? v)
    {
        try
        {
            v = JsonSerializer.Deserialize<dynamic>(strInput) ?? throw new Exception("Couldn't deserialize JSON");

            return true;
        }
        catch (Exception)
        {
        }

        v = null;
        return false;
    }
}
