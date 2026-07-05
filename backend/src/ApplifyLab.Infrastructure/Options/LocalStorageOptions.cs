namespace ApplifyLab.Infrastructure.Options;

public class LocalStorageOptions
{
    public const string SectionName = "LocalStorage";

    /// <summary>Absolute or relative-to-content-root path of the folder files are written to.</summary>
    public string RootPath { get; set; } = "uploads";

    /// <summary>Base URL the API serves that folder back over HTTP (see Program.cs UseStaticFiles mapping).</summary>
    public string PublicBaseUrl { get; set; } = default!;
}
