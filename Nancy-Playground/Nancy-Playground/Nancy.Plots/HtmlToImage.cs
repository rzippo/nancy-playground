using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Spectre.Console;

public static class HtmlToImage
{
    /// <summary>
    /// Renders HTML to an image using Playwright headless Chromium.
    /// </summary>
    /// <param name="html">Full HTML markup (you can include <style> and inline images).</param>
    /// <param name="width">Viewport width in CSS px.</param>
    /// <param name="height">Viewport height in CSS px. If null, use fullPage screenshot.</param>
    /// <param name="format">"png" or "jpeg".</param>
    /// <param name="quality">JPEG quality 0–100 (ignored for PNG).</param>
    /// <param name="background">If true, keeps page background; false gives transparent PNG.</param>
    /// <returns>Byte[] of the image.</returns>
    [SuppressMessage("Usage", "NANCY0005:Division between decimal and/or integer types may lose precision")]
    public static async Task<byte[]> RenderAsync(
        string html,
        int width = 1240,
        int? height = null,
        string format = "png",
        int? quality = null,
        bool background = true)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height ?? ((width / 16) * 9)  }
        });

        // Base URL is helpful if your HTML uses relative URLs
        await page.SetContentAsync(html, new PageSetContentOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        var screenshotOptions = new PageScreenshotOptions
        {
            FullPage = height is null, // capture full height if not specified
            Type = format.Equals("jpeg", StringComparison.OrdinalIgnoreCase) ? 
                ScreenshotType.Jpeg : 
                ScreenshotType.Png,
            OmitBackground = !background
        };
        if (quality.HasValue && screenshotOptions.Type == ScreenshotType.Jpeg)
            screenshotOptions.Quality = quality;

        return await page.ScreenshotAsync(screenshotOptions);
    }

    /// <summary>
    /// Image export requires Firefox to be installed.
    /// </summary>
    public static void InstallBrowser()
    {
        int exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new Exception($"Failed to install Playwright with exit code {exitCode}.");
        }
    }
}
