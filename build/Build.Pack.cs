using System;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Definition;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;

using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.NuGet.NuGetTasks;

using Project = Microsoft.Build.Evaluation.Project;

public partial class Build
{
    // logic from 01_Build.bat
    Target Pack => _ => _
        .DependsOn(Compile)
        .After(Test)
        .OnlyWhenDynamic(() => IsRunningOnWindows)
        .Executes(() =>
        {
            if (Configuration != Configuration.Release)
            {
                throw new InvalidOperationException("Cannot pack if compilation hasn't been done in Release mode, use --configuration Release");
            }

            var nugetVersion = VersionPrefix;
            if (!string.IsNullOrWhiteSpace(VersionSuffix))
            {
                nugetVersion += "-" + VersionSuffix;
            }

            // it seems to cause some headache with publishing, so let's dotnet pack only files we know are suitable
            var projects = SourceDirectory.GlobFiles("**/*.csproj")
                .Where(x => !x.ToString().Contains("_build") &&
                            !x.ToString().Contains("NSwag.Console.csproj") && // legacy .net tool is not published as nuget package
                            !x.ToString().Contains("NSwagStudio") &&
                            !x.ToString().Contains("Test") &&
                            !x.ToString().Contains("Demo") &&
                            !x.ToString().Contains("Integration") &&
                            !x.ToString().Contains("x86") &&
                            !x.ToString().Contains("Launcher") &&
                            !x.ToString().Contains("Sample"))
                .Select(x => Project.FromFile(x, new ProjectOptions()));

            foreach (var project in projects)
            {
                DotNetPack(s => s
                    .SetProcessWorkingDirectory(SourceDirectory)
                    .SetProject(project.FullPath)
                    .SetAssemblyVersion(VersionPrefix)
                    .SetFileVersion(VersionPrefix)
                    .SetInformationalVersion(VersionPrefix)
                    .SetVersion(nugetVersion)
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(ArtifactsDirectory)
                    .EnableNoRestore()
                    .EnableNoBuild()
                );
            }

            Serilog.Log.Information("Build WiX installer");

            (SourceDirectory / "NSwagStudio.Installer" / "bin").CreateOrCleanDirectory();

            DotNetBuild(x => x
                .SetProjectFile(GetProject("NSwagStudio.Installer"))
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetVerbosity(DotNetVerbosity.minimal)
            );

            // gather relevant artifacts
            Serilog.Log.Information("Package nuspecs");

            var apiDescriptionClientNuSpec = SourceDirectory / "NSwag.ApiDescription.Client" / "NSwag.ApiDescription.Client.nuspec";
            var content = apiDescriptionClientNuSpec.ReadAllText();
            content = Regex.Replace(
                content,
                "<dependency id=\"Monigass\\.NSwag\\.MSBuild\" version=\"[^\"]*\" />",
                "<dependency id=\"Monigass.NSwag.MSBuild\" version=\"" + nugetVersion + "\" />");
            apiDescriptionClientNuSpec.WriteAllText(content);

            var nuspecs = new[]
            {
                apiDescriptionClientNuSpec,
                SourceDirectory / "NSwag.MSBuild" / "NSwag.MSBuild.nuspec",
                SourceDirectory / "NSwagStudio.Chocolatey" / "NSwagStudio.nuspec"
            };

            foreach (var nuspec in nuspecs)
            {
                ValidateNuspecMetadata(nuspec);
            }

            foreach (var nuspec in nuspecs)
            {
                NuGetPack(x => x
                    .SetOutputDirectory(ArtifactsDirectory)
                    .SetConfiguration(Configuration)
                    .SetVersion(nugetVersion)
                    .SetTargetPath(nuspec)
                );
            }

            var artifacts = Array.Empty<AbsolutePath>()
                .Concat(RootDirectory.GlobFiles("**/Release/**/Monigass.NSwag*.nupkg"))
                .Concat(SourceDirectory.GlobFiles("**/Release/**/NSwagStudio.msi"));

            foreach (var artifact in artifacts)
            {
                artifact.CopyToDirectory(ArtifactsDirectory);
            }

            // patch npm version
            var npmPackagesFile = SourceDirectory / "NSwag.Npm" / "package.json";
            content = npmPackagesFile.ReadAllText();
            content = Regex.Replace(content, @"""version"": "".*""", @"""version"": """ + nugetVersion + @"""");
            npmPackagesFile.WriteAllText(content);

            // ZIP directories
            ZipFile.CreateFromDirectory(NSwagNpmBinaries, ArtifactsDirectory / "NSwag.Npm.zip");
            ZipFile.CreateFromDirectory(NSwagStudioBinaries, ArtifactsDirectory / "NSwag.zip");

            // NSwagStudio.msi
            (ArtifactsDirectory / "bin" / "NSwagStudio.Installer" / Configuration / "NSwagStudio.msi").CopyToDirectory(ArtifactsDirectory);
        });

    static void ValidateNuspecMetadata(AbsolutePath nuspec)
    {
        var content = nuspec.ReadAllText();

        if (content.Contains("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Nuspec {nuspec} uses raw.githubusercontent.com for iconUrl. Use CDN-hosted icon URL.");
        }

        if (!content.Contains("<authors>Rico Suter</authors>", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Nuspec {nuspec} must set <authors>Rico Suter</authors>.");
        }

        if (!content.Contains("<owners>Monigass</owners>", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Nuspec {nuspec} must set <owners>Monigass</owners>.");
        }

        if (!Regex.IsMatch(content, "<description>.*fork.*</description>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            throw new InvalidOperationException($"Nuspec {nuspec} description must disclose that it is a fork.");
        }

        if (!Regex.IsMatch(content, "<description>.*not the upstream.*</description>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            throw new InvalidOperationException($"Nuspec {nuspec} description must explicitly state it is not the upstream package.");
        }
    }
}

