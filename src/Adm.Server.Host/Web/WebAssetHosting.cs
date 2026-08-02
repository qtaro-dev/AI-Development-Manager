using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Adm.Server.Host.Web;

public static class WebAssetHosting
{
    public const string DefaultWebRootName = "wwwroot";

    public static string GetDefaultWebRoot() =>
        Path.Combine(
            Path.GetDirectoryName(typeof(WebAssetHosting).Assembly.Location)
                ?? AppContext.BaseDirectory,
            DefaultWebRootName);

    public static void ValidateWebRoot(string webRoot)
    {
        if (!File.Exists(Path.Combine(webRoot, "index.html")))
        {
            throw new InvalidOperationException(
                "Web UIの配布成果物が見つかりません。Serverと同じ配布物へWeb buildを含めてください。");
        }
    }

    public static void UseWebAssets(WebApplication app, string webRoot)
    {
        ValidateWebRoot(webRoot);

        var fileProvider = new PhysicalFileProvider(webRoot);
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = fileProvider,
            DefaultFileNames = ["index.html"]
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            OnPrepareResponse = context => ApplyCachePolicy(context.Context, context.File.Name)
        });
    }

    public static void MapSpaFallback(WebApplication app, string webRoot)
    {
        var indexPath = Path.Combine(webRoot, "index.html");

        app.MapFallback(async context =>
        {
            if (IsReservedPath(context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers.CacheControl = "no-cache, no-store";
            await context.Response.SendFileAsync(indexPath);
        });
    }

    private static bool IsReservedPath(PathString path) =>
        path.StartsWithSegments("/api") ||
        path.StartsWithSegments("/health") ||
        path.StartsWithSegments("/openapi");

    private static void ApplyCachePolicy(HttpContext context, string fileName)
    {
        if (fileName.Equals("index.html", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers.CacheControl = "no-cache, no-store";
            return;
        }

        if (HasContentHash(fileName))
        {
            context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return;
        }

        context.Response.Headers.CacheControl = "no-cache";
    }

    private static bool HasContentHash(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var separator = stem.LastIndexOf('-');
        return separator >= 0 && stem.Length - separator - 1 >= 6;
    }
}
