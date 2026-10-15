class Dotnet < Formula
  desc ".NET Core"
  homepage "https://dotnet.microsoft.com/"
  version "10.0.401"
  url "https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.401/dotnet-sdk-10.0.401-win-x64.zip"
  sha256 "c1b96dea223e1ab2af8a51187e0cc96338d14c68834be1656136be18953cea09"
  license "MIT"

  livecheck do
    url "https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json"
    regex(/"latest-sdk"\s*:\s*"(\d+(?:\.\d+)+)"/i)
  end

  # Official .NET SDK zip from Microsoft; dotnet.exe finds its SDKs and runtimes in place.

  def install
    install_tree
    windows_exe "libexec/dotnet.exe", "dotnet"
  end

  test do
    target_framework = "net#{version.major_minor}"

    (testpath/"test.cs").write <<~CSHARP
      using System;

      namespace Homebrew
      {
        public class Dotnet
        {
          public static void Main(string[] args)
          {
            Console.WriteLine(String.Join(",", args));
          }
        }
      }
    CSHARP

    (testpath/"test.csproj").write <<~XML
      <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup>
          <OutputType>Exe</OutputType>
          <TargetFrameworks>#{target_framework}</TargetFrameworks>
        </PropertyGroup>
      </Project>
    XML

    system bin/"dotnet", "build", "--framework", target_framework, "--output", testpath, testpath/"test.csproj"
    output = shell_output("#{bin}/dotnet run --framework #{target_framework} #{testpath}/test.dll a b c")
    assert_equal "#{testpath}/test.dll,a,b,c", output.chomp
  end
end
