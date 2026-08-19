namespace Shaunebu.MAUI.NFC.Tests;

internal static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();

    public static string LibraryProject => Path.Combine(Root, "src", "Shaunebu.MAUI.NFC", "Shaunebu.MAUI.NFC.csproj");

    public static string PackageDirectory => Path.Combine(Root, "src", "Shaunebu.MAUI.NFC", "bin", "Release");

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Shaunebu.MAUI.NFC", "Shaunebu.MAUI.NFC.csproj");
            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
