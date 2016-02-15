#tool nuget:?package=ILRepack&version=2.0.10

using System.Text.RegularExpressions;

var target = Argument<string>("target", "Build");
var configuration = Argument<string>("configuration", "Debug");
var solution = Argument<string>("solution", "CKAN.sln");

Task("BuildDotNet")
    .Does(() =>
{
    var version = GetGitVersion();

    var metaFileContents = TransformTextFile("Core/Meta.cs.in")
        .WithToken("version", version == null ? "null" : string.Format(@"""{0}""", version))
        .ToString();

    System.IO.File.WriteAllText("Core/Meta.cs", metaFileContents);

    DotNetBuild(solution, settings =>
    {
        settings.Properties["win32icon"] = new List<string> { MakeAbsolute(File("GUI/assets/ckan.ico")).FullPath };

        if (IsStable())
        {
            settings.Properties["DefineConstants"] = new List<string> { "STABLE" };
        }
    });
});

Task("BuildCkan")
    .IsDependentOn("BuildDotNet")
    .Does(() =>
{
    var assemblyPaths = GetFiles(string.Format("Cmdline/bin/{0}/*.dll", configuration));
    assemblyPaths.Add(string.Format("Cmdline/bin/{0}/CKAN-GUI.exe", configuration));

    ILRepack("ckan.exe", string.Format("Cmdline/bin/{0}/CmdLine.exe", configuration), assemblyPaths,
        new ILRepackSettings
        {
            Libs = new List<FilePath> { string.Format("Cmdline/bin/{0}", configuration) },
            // TODO: Cannot use the "TargetPlaform"
            // Must wait until https://github.com/cake-build/cake/pull/692 is released.
            ArgumentCustomization = builder => {
                builder.Append("/targetplatform:v4");
                return builder;
            }
        }
    );
});

Task("BuildNetkan")
    .IsDependentOn("BuildDotNet")
    .Does(() =>
{
    ILRepack(
        "netkan.exe",
        string.Format("Netkan/bin/{0}/NetKAN.exe", configuration),
        GetFiles(string.Format("Netkan/bin/{0}/*.dll", configuration)),
        new ILRepackSettings
        {
            Libs = new List<FilePath> { string.Format("Netkan/bin/{0}", configuration) }
        }
    );
});

Task("Build")
    .IsDependentOn("BuildCkan")
    .IsDependentOn("BuildNetkan");

RunTarget(target);

private bool IsStable()
{
    var processSettings = new ProcessSettings
    {
        Arguments = "rev-parse --abbrev-ref HEAD",
        RedirectStandardOutput = true
    };

    IEnumerable<string> output;
    if (StartProcess("git", processSettings, out output) == 0)
    {
        var branch = output.Single();
        return Regex.IsMatch(branch, @"(\b|_)stable(\b|_)") || Regex.IsMatch(branch, @"v\d+\.\d*[02468]$");
    }
    else
    {
        return false;
    }
}

private string GetGitVersion()
{
    var processSettings = new ProcessSettings
    {
        Arguments = "describe --tags --long",
        RedirectStandardOutput = true
    };

    IEnumerable<string> output;
    if (StartProcess("git", processSettings, out output) == 0)
        return output.Single();
    else
        return null;
}
