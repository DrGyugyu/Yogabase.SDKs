using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Xml.Linq;

namespace Yogabase.Tests;

/// <summary>
/// The Unity SDK is a separate netstandard2.1 build pinned to the 8.0 package line (owner decision,
/// 2026-09-17 afternoon), so an IL2CPP player never meets a member that exists only in a 9.x or 10.x
/// assembly. Unity 6000.5.7f1 binds its own 8.0 BCL extensions whatever a plugin was built against,
/// and a plugin built against a higher version is CS1705 in every Unity-compiled assembly that uses
/// it — the failure that made the demo read its realtime payload without naming a System.Text.Json 10
/// member. These tests pin the build's inputs (its locks and pins) and its output (the references the
/// built assemblies actually carry), and that it compiles the very same sources as the NuGet build.
/// </summary>
public sealed class UnityBuildTests
{
    private const string SolutionFileName = "Yogabase.SDKs.slnx";

    public static TheoryData<string, string> UnityBuilds() => new()
    {
        { "Yogabase.Yogabase.Client.Unity", "Yogabase.Yogabase.Client" },
        { "Yogabase.Yogabase.RuntimeHost.Unity", "Yogabase.Yogabase.RuntimeHost" },
    };

    [Theory]
    [MemberData(nameof(UnityBuilds))]
    public void UnityBuildReferencesNoAssemblyAbove8(string project, string assemblyName)
    {
        var path = BuiltAssembly(project, assemblyName);
        using var stream = File.OpenRead(path);
        using var reader = new PEReader(stream);
        var metadata = reader.GetMetadataReader();

        var references = metadata.AssemblyReferences
            .Select(handle => metadata.GetAssemblyReference(handle))
            .Select(reference => (Name: metadata.GetString(reference.Name), reference.Version))
            .ToList();

        // Guard against a vacuous pass: the client must reference SignalR and System.Text.Json at all.
        Assert.Contains(references, reference => reference.Name == "System.Text.Json");
        if (assemblyName == "Yogabase.Yogabase.Client")
        {
            Assert.Contains(references, reference => reference.Name == "Microsoft.AspNetCore.SignalR.Client");
            Assert.Contains(references, reference => reference.Name == "Microsoft.AspNetCore.SignalR.Client.Core");
        }

        var above8 = references.Where(reference => reference.Version.Major >= 9)
            .Select(reference => $"{reference.Name} {reference.Version}")
            .ToList();
        Assert.True(above8.Count == 0, $"{assemblyName} (Unity build) references 9.x/10.x assemblies: {string.Join(", ", above8)}");
        Assert.Equal(".NETStandard,Version=v2.1", TargetFramework(metadata));
    }

    [Theory]
    [MemberData(nameof(UnityBuilds))]
    public void UnityBuildLockResolvesNothingAbove8(string project, string assemblyName)
    {
        _ = assemblyName;
        var lockPath = Path.Combine(RepoRoot(), "src", "unity", project, "packages.lock.json");
        using var document = JsonDocument.Parse(File.ReadAllText(lockPath));
        var frameworks = document.RootElement.GetProperty("dependencies").EnumerateObject().ToList();

        Assert.Equal([".NETStandard,Version=v2.1"], frameworks.Select(framework => framework.Name));
        var resolved = frameworks.Single().Value.EnumerateObject()
            .Select(package => (Id: package.Name, Version: Version.Parse(package.Value.GetProperty("resolved").GetString()!.Split('-')[0])))
            .ToList();
        Assert.Contains(resolved, package => package.Id == "System.Text.Json" && package.Version.Major == 8);
        var above8 = resolved.Where(package => package.Version.Major >= 9).Select(package => $"{package.Id} {package.Version}").ToList();
        Assert.True(above8.Count == 0, $"{project} resolves 9.x/10.x packages: {string.Join(", ", above8)}");
    }

    [Fact]
    public void TheUnityPackageLineIsPinnedTo8AndIsolatedFromTheRootPins()
    {
        var root = RepoRoot();
        var pins = XDocument.Load(Path.Combine(root, "src", "unity", "Directory.Packages.props"))
            .Descendants("PackageVersion")
            .ToDictionary(element => (string)element.Attribute("Include")!, element => (string)element.Attribute("Version")!);

        // Exactly the 8.0 line the AI Game Developer package restores, so the demo's own copies and
        // that package's copies are the same bytes (the demo's plugin lock records both digests).
        Assert.Equal("8.0.15", pins["Microsoft.AspNetCore.SignalR.Client"]);
        Assert.Equal("8.0.5", pins["System.Text.Json"]);
        Assert.All(pins.Values, version => Assert.StartsWith("8.", version, StringComparison.Ordinal));

        // src/unity/Directory.Packages.props must not import the root one: MSBuild would then apply
        // the root's 10.x pins and transitive pinning to the Unity build.
        var text = File.ReadAllText(Path.Combine(root, "src", "unity", "Directory.Packages.props"));
        Assert.DoesNotContain("<Import", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Yogabase.Yogabase.Client.Unity", "Yogabase.Yogabase.Client", "Yogabase.Yogabase.Client")]
    [InlineData("Yogabase.Yogabase.RuntimeHost.Unity", "Yogabase.Yogabase.RuntimeHost", "Yogabase.Yogabase.RuntimeHost")]
    public void UnityBuildCompilesTheSameSourcesAndAssemblyNameAsTheNuGetBuild(string unityProject, string dotnetProject, string assemblyName)
    {
        var root = RepoRoot();
        var project = XDocument.Load(Path.Combine(root, "src", "unity", unityProject, unityProject + ".csproj"));
        Assert.Equal(assemblyName, project.Descendants("AssemblyName").Single().Value);
        Assert.Equal("netstandard2.1", project.Descendants("TargetFramework").Single().Value);
        Assert.Equal($"../../dotnet/{dotnetProject}/*.cs", Assert.Single(project.Descendants("Compile")).Attribute("Include")!.Value);

        // The glob is one folder deep; a source the NuGet build compiles from a subfolder would be
        // silently missing from the Unity build.
        var sources = Path.Combine(root, "src", "dotnet", dotnetProject);
        var nested = Directory.EnumerateFiles(sources, "*.cs", SearchOption.AllDirectories)
            .Where(path => Path.GetDirectoryName(path) != sources)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();
        Assert.Empty(nested);
    }

    private static string TargetFramework(MetadataReader metadata)
    {
        foreach (var handle in metadata.GetAssemblyDefinition().GetCustomAttributes())
        {
            var attribute = metadata.GetCustomAttribute(handle);
            if (attribute.Constructor.Kind != HandleKind.MemberReference)
            {
                continue;
            }

            var constructor = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
            var type = metadata.GetTypeReference((TypeReferenceHandle)constructor.Parent);
            if (metadata.GetString(type.Name) != "TargetFrameworkAttribute")
            {
                continue;
            }

            var blob = metadata.GetBlobReader(attribute.Value);
            blob.ReadUInt16(); // prolog
            return blob.ReadSerializedString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string BuiltAssembly(string project, string assemblyName)
    {
        // The test project references both Unity projects with ReferenceOutputAssembly="false", so a
        // build of this project builds them first, in the same configuration.
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var path = Path.Combine(RepoRoot(), "src", "unity", project, "bin", configuration, "netstandard2.1", assemblyName + ".dll");
        Assert.True(File.Exists(path), $"The Unity build output is missing: {path}. Build this test project (it builds {project}).");
        return path;
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not locate {SolutionFileName} from {AppContext.BaseDirectory}.");
    }
}
