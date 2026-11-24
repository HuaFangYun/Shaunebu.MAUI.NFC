using System.Xml.Linq;

namespace Shaunebu.MAUI.NFC.Tests;

public sealed class ProjectMetadataTests
{
    [Fact]
    public void PublishedInterfaceCompatibilityMemberIsPresent()
    {
        var interfaceSource = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", "Shaunebu.MAUI.NFC", "INFC.cs"));

        Assert.Contains("void RaiseMessageReceived(ITagInfo tagInfo);", interfaceSource);
    }

    [Fact]
    public void PackageIncludesReadmeAndIconAssets()
    {
        var project = XDocument.Load(RepositoryPaths.LibraryProject);
        var packageAssets = project.Descendants("None")
            .Select(element => new
            {
                Include = element.Attribute("Include")?.Value,
                Pack = element.Attribute("Pack")?.Value,
                PackagePath = element.Attribute("PackagePath")?.Value
            })
            .ToArray();

        Assert.Contains(packageAssets, asset =>
            asset.Include == @"..\..\README.md" &&
            asset.Pack == "true" &&
            asset.PackagePath == @"\");

        Assert.Contains(packageAssets, asset =>
            asset.Include == @"..\..\art\nfc128.png" &&
            asset.Pack == "true" &&
            asset.PackagePath == "icon.png");
    }
}
