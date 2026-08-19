using System.Xml.Linq;

namespace Shaunebu.MAUI.NFC.Tests;

public sealed class ProjectMetadataTests
{
    [Fact]
    public void PackageMetadataTargetsVersionAndPublishedBaseline()
    {
        var project = XDocument.Load(RepositoryPaths.LibraryProject);
        var properties = project.Descendants("PropertyGroup").Elements()
            .GroupBy(element => element.Name.LocalName)
            .ToDictionary(group => group.Key, group => group.Last().Value);

        Assert.Equal("net9.0-android35.0;net9.0-ios18.0;net10.0-android36.0;net10.0-ios26.0", properties["TargetFrameworks"]);
        Assert.Equal("1.1.0", properties["Version"]);
        Assert.Equal("true", properties["EnablePackageValidation"]);
        Assert.Equal("1.0.4", properties["PackageValidationBaselineVersion"]);
        Assert.Equal("icon.png", properties["PackageIcon"]);
        Assert.Equal("README.md", properties["PackageReadmeFile"]);
    }

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
