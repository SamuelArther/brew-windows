class CyclonedxCli < Formula
  desc "Tool for analysis and manipulation of CycloneDX SBOMs"
  homepage "https://cyclonedx.org/"
  url "https://github.com/CycloneDX/cyclonedx-cli/archive/refs/tags/v0.33.1.tar.gz"
  sha256 "01574e2e7d1cfe88ed50bbe7e87663af45a6b261ba11518a6c8eb34f809e1309"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "ceda941e37bf71377793905c8aab8928d9e0b979a63f4b7318de74677326643d"
  end

  depends_on "dotnet"

  resource "document.spdx.json", :test do
    url "https://raw.githubusercontent.com/CycloneDX/cyclonedx-cli/refs/tags/v0.32.0/tests/cyclonedx.tests/Resources/document.spdx.json"
    sha256 "6fed40c4b4774821c2a9002b3ad44c1234987ff5d7780345ed29b01e942b8142"
  end

  def install
    dotnet = Formula["dotnet"]
    args = %W[
      --configuration Release
      --framework net#{dotnet.version.major_minor}
      --no-self-contained
      --output #{libexec}
      --use-current-runtime
      -p:AppHostRelativeDotNet=#{dotnet.opt_libexec.relative_path_from(libexec)}
      -p:DebugSymbols=false
      -p:DebugType=None
      -p:PublishSingleFile=true
      -p:PublishTrimmed=false
      -p:Version=#{version}
    ]
    system "dotnet", "publish", "src/cyclonedx/cyclonedx.csproj", *args
    bin.install_symlink libexec/"cyclonedx"
  end

  test do
    testpath.install resource("document.spdx.json")
    system bin/"cyclonedx", "convert", "--input-file=document.spdx.json", "--output-file=bom.cdx.json"
    system bin/"cyclonedx", "validate", "--input-file=bom.cdx.json"

    assert_equal version.to_s, shell_output("#{bin}/cyclonedx --version").strip
  end
end
