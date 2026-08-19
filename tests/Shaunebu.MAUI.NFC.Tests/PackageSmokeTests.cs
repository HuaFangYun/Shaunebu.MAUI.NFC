using System.Diagnostics;
using System.IO.Compression;

namespace Shaunebu.MAUI.NFC.Tests;

#if NET10_0_OR_GREATER
public sealed class PackageSmokeTests
{
    [Fact]
    public async Task LocalPackageInstallsRestoresAndCompilesAndroidConsumersAndContainsIosAssets()
    {
        var smokeRoot = Path.Combine(Path.GetTempPath(), "nfc-smoke", Guid.NewGuid().ToString("N")[..8]);
        var packageOutput = Path.Combine(smokeRoot, "packages");
        var consumerRoot = Path.Combine(smokeRoot, "consumer");

        Directory.CreateDirectory(packageOutput);
        Directory.CreateDirectory(consumerRoot);

        try
        {
            await RunDotnetAsync(["pack", RepositoryPaths.LibraryProject, "-c", "Release", "-o", packageOutput]);

            var packagePath = Path.Combine(packageOutput, "Shaunebu.MAUI.NFC.1.1.0.nupkg");
            Assert.True(File.Exists(packagePath), $"Expected package at {packagePath}");
            Assert.True(new FileInfo(packagePath).Length > 0);

            using (var archive = ZipFile.OpenRead(packagePath))
            {
                Assert.Contains(archive.Entries, entry => entry.FullName == "lib/net9.0-android35.0/Shaunebu.MAUI.NFC.dll");
                Assert.Contains(archive.Entries, entry => entry.FullName == "lib/net9.0-ios18.0/Shaunebu.MAUI.NFC.dll");
                Assert.Contains(archive.Entries, entry => entry.FullName == "lib/net10.0-android36.0/Shaunebu.MAUI.NFC.dll");
                Assert.Contains(archive.Entries, entry => entry.FullName == "lib/net10.0-ios26.0/Shaunebu.MAUI.NFC.dll");
                Assert.Contains(archive.Entries, entry => entry.FullName == "lib/net9.0-android35.0/Shaunebu.MAUI.NFC.xml");
                Assert.Contains(archive.Entries, entry => entry.FullName == "lib/net10.0-android36.0/Shaunebu.MAUI.NFC.xml");
                Assert.Contains(archive.Entries, entry => entry.FullName == "README.md");
                Assert.Contains(archive.Entries, entry => entry.FullName == "icon.png");
                Assert.Contains(archive.Entries, entry => entry.FullName == "Shaunebu.MAUI.NFC.nuspec");
            }

            WriteConsumerProjects(consumerRoot, packageOutput);

            foreach (var consumerProject in Directory
                         .EnumerateFiles(consumerRoot, "*.csproj", SearchOption.AllDirectories)
                         .Where(project => !project.Contains("ios-", StringComparison.OrdinalIgnoreCase)))
            {
                var restoreArguments = new List<string> { "restore", consumerProject };
                var buildArguments = new List<string> { "build", consumerProject, "-c", "Release", "--no-restore" };

                if (consumerProject.Contains("ios-", StringComparison.OrdinalIgnoreCase))
                {
                    restoreArguments.Add("/p:RuntimeIdentifier=iossimulator-x64");
                    buildArguments.Add("/p:RuntimeIdentifier=iossimulator-x64");
                }

                await RunDotnetAsync([.. restoreArguments], consumerRoot);
                await RunDotnetAsync([.. buildArguments], consumerRoot);
            }
        }
        finally
        {
            if (Directory.Exists(smokeRoot))
            {
                Directory.Delete(smokeRoot, recursive: true);
            }

            DeleteIfEmpty(Path.Combine(Path.GetTempPath(), "nfc-smoke"));
        }
    }

    private static void WriteConsumerProjects(string consumerRoot, string packageOutput)
    {
        var androidNet9Root = Path.Combine(consumerRoot, "android-net9");
        var iosNet9Root = Path.Combine(consumerRoot, "ios-net9");
        var androidNet10Root = Path.Combine(consumerRoot, "android-net10");
        var iosNet10Root = Path.Combine(consumerRoot, "ios-net10");
        Directory.CreateDirectory(androidNet9Root);
        Directory.CreateDirectory(iosNet9Root);
        Directory.CreateDirectory(androidNet10Root);
        Directory.CreateDirectory(iosNet10Root);

        var escapedPackageOutput = packageOutput.Replace(@"\", @"\\");
        File.WriteAllText(
            Path.Combine(consumerRoot, "NuGet.config"),
            $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <add key="local" value="{{escapedPackageOutput}}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """);

        File.WriteAllText(
            Path.Combine(androidNet9Root, "PackageSmokeConsumer.Android.Net9.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0-android35.0</TargetFramework>
                <UseMaui>true</UseMaui>
                <SingleProject>true</SingleProject>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <CheckEolTargetFramework>false</CheckEolTargetFramework>
                <CheckEolWorkloads>false</CheckEolWorkloads>
                <SupportedOSPlatformVersion>21.0</SupportedOSPlatformVersion>
                <AssetTargetFallback>$(AssetTargetFallback);net9.0-android35.0</AssetTargetFallback>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.Maui.Controls" Version="9.0.120" />
                <PackageReference Include="Shaunebu.MAUI.NFC" Version="1.1.0" />
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(
            Path.Combine(iosNet9Root, "PackageSmokeConsumer.iOS.Net9.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0-ios18.0</TargetFramework>
                <UseMaui>true</UseMaui>
                <SingleProject>true</SingleProject>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <CheckEolTargetFramework>false</CheckEolTargetFramework>
                <CheckEolWorkloads>false</CheckEolWorkloads>
                <AssetTargetFallback>$(AssetTargetFallback);net9.0-ios18.0</AssetTargetFallback>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.Maui.Controls" Version="9.0.120" />
                <PackageReference Include="Shaunebu.MAUI.NFC" Version="1.1.0" />
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(
            Path.Combine(androidNet10Root, "PackageSmokeConsumer.Android.Net10.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0-android36.0</TargetFramework>
                <UseMaui>true</UseMaui>
                <SingleProject>true</SingleProject>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <SupportedOSPlatformVersion>21.0</SupportedOSPlatformVersion>
                <AssetTargetFallback>$(AssetTargetFallback);net10.0-android36.0</AssetTargetFallback>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.Maui.Controls" Version="10.0.20" />
                <PackageReference Include="Shaunebu.MAUI.NFC" Version="1.1.0" />
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(
            Path.Combine(iosNet10Root, "PackageSmokeConsumer.iOS.Net10.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0-ios26.0</TargetFramework>
                <UseMaui>true</UseMaui>
                <SingleProject>true</SingleProject>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <AssetTargetFallback>$(AssetTargetFallback);net10.0-ios26.0</AssetTargetFallback>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.Maui.Controls" Version="10.0.20" />
                <PackageReference Include="Shaunebu.MAUI.NFC" Version="1.1.0" />
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(
            Path.Combine(androidNet9Root, "ConsumerCompileCheck.cs"),
            """
            using Shaunebu.MAUI.NFC;

            namespace PackageSmokeConsumer;

            public static class ConsumerCompileCheck
            {
                public static bool IsPackageVisible() => CrossNFC.IsSupported || !CrossNFC.IsSupported;
            }
            """);

        File.WriteAllText(
            Path.Combine(iosNet9Root, "ConsumerCompileCheck.cs"),
            """
            using Shaunebu.MAUI.NFC;

            namespace PackageSmokeConsumer;

            public static class ConsumerCompileCheck
            {
                public static bool IsPackageVisible() => CrossNFC.IsSupported || !CrossNFC.IsSupported;
            }
            """);

        File.WriteAllText(
            Path.Combine(androidNet10Root, "ConsumerCompileCheck.cs"),
            """
            using Shaunebu.MAUI.NFC;

            namespace PackageSmokeConsumer;

            public static class ConsumerCompileCheck
            {
                public static bool IsPackageVisible() => CrossNFC.IsSupported || !CrossNFC.IsSupported;
            }
            """);

        File.WriteAllText(
            Path.Combine(iosNet10Root, "ConsumerCompileCheck.cs"),
            """
            using Shaunebu.MAUI.NFC;

            namespace PackageSmokeConsumer;

            public static class ConsumerCompileCheck
            {
                public static bool IsPackageVisible() => CrossNFC.IsSupported || !CrossNFC.IsSupported;
            }
            """);
    }

    private static async Task RunDotnetAsync(string[] arguments, string? workingDirectory = null)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory ?? RepositoryPaths.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start dotnet process.");

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(5));

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet {string.Join(' ', arguments)} failed with exit code {process.ExitCode}.{Environment.NewLine}{await standardOutput}{await standardError}");
        }
    }

    private static void DeleteIfEmpty(string directory)
    {
        if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }
    }
}
#endif
