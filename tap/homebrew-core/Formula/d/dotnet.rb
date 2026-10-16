class Dotnet < Formula
  desc ".NET Core"
  homepage "https://dotnet.microsoft.com/"
  url "https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.402/dotnet-sdk-10.0.402-win-x64.zip"
  version "10.0.402"
  sha256 "24d057ab968986a9d7c7a01606b2a0192cbe6485221ba38e9c685522e93d8798"
  license "MIT"
  version_scheme 1

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
    assert_match version.to_s, shell_output("#{bin}/dotnet --version")

    target_framework = "net#{version.major_minor}"

    (testpath/"test.cs").write <<~CS
      using System;

      namespace Homebrew
      {
        public class Dotnet
        {
          public static void Main(string[] args)
          {
            var joined = String.Join(",", args);
            Console.WriteLine(joined);
          }
        }
      }
    CS

    (testpath/"test.csproj").write <<~XML
      <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup>
          <OutputType>Exe</OutputType>
          <TargetFrameworks>#{target_framework}</TargetFrameworks>
          <PlatformTarget>AnyCPU</PlatformTarget>
          <RootNamespace>Homebrew</RootNamespace>
          <PackageId>Homebrew.Dotnet</PackageId>
          <Title>Homebrew.Dotnet</Title>
          <Product>$(AssemblyName)</Product>
          <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
        </PropertyGroup>
        <ItemGroup>
          <Compile Include="test.cs" />
        </ItemGroup>
      </Project>
    XML

    # No build servers, so nothing keeps running from the keg after the test.
    system bin/"dotnet", "build", "--disable-build-servers", "--framework", target_framework,
           "--output", testpath, testpath/"test.csproj"
    output = shell_output("#{bin}/dotnet run --disable-build-servers --framework #{target_framework} " \
                          "#{testpath}/test.dll a b c")
    assert_equal "#{testpath}/test.dll,a,b,c\n", output
  end
end
