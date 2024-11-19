#r "nuget: SimpleExec, 6.2.0"
#r "nuget: Newtonsoft.Json, 12.0.3"

using System;
using System.IO;
using Newtonsoft.Json;

if (Args.Count < 2)
{
    Console.WriteLine("Usage: build.csx <OutputFile> <BuildConfiguration>");
    return 1;
}

string outFile = Args[0];
string buildConfig = Args[1];

string buildFile = "build_number.txt";
string branchName = "master"; // Default branch
string commitSha = "unknown"; // Default SHA
string major = "0";
string minor = "13";

// Increment the build number
int buildNumber = 1;
if (File.Exists(buildFile))
{
    buildNumber = int.Parse(File.ReadAllText(buildFile)) + 1;
}
File.WriteAllText(buildFile, buildNumber.ToString());

// Attempt to get the current Git commit SHA
try
{
    commitSha = SimpleExec.Command.Read("git", "rev-parse --short HEAD").Trim();
    branchName = SimpleExec.Command.Read("git", "rev-parse --abbrev-ref HEAD").Trim();
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Could not fetch Git data. {ex.Message}");
}

// Generate version information
string version = $"{major}.{minor}.{buildNumber}-{commitSha}";

var genFile = $@"
[assembly: System.Reflection.AssemblyVersion(""{major}.{minor}.{buildNumber}"")]
[assembly: System.Reflection.AssemblyFileVersion(""{major}.{minor}.{buildNumber}"")]
[assembly: System.Reflection.AssemblyInformationalVersion(""{version}"")]

namespace TS3AudioBot.Environment
{{
    partial class BuildData {{
        partial void GetDataInternal() {{
            this.Version = ""{version}"";
            this.Branch = ""{branchName}"";
            this.CommitSha = ""{commitSha}"";

            this.BuildConfiguration = ""{buildConfig}"";
        }}
    }}
}}
";

Console.WriteLine($"Generated Version: {version}");
var writeFull = Path.GetFullPath(outFile);
File.WriteAllText(writeFull, genFile);
Console.WriteLine($"Version file written to: {writeFull}");
