class Pulumi < Formula
  desc "Cloud native development platform"
  homepage "https://www.pulumi.com/"
  url "https://github.com/pulumi/pulumi/releases/download/v3.268.0/pulumi-v3.268.0-windows-x64.zip"
  sha256 "c7c3baabb2cf80d1edfdf9255c6aede9c0adfccedf1d98d2e8ec149211fc223e"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/bin/pulumi.exe", "pulumi"
    windows_exe "libexec/bin/pulumi-language-dotnet.exe", "pulumi-language-dotnet"
    windows_exe "libexec/bin/pulumi-language-go.exe", "pulumi-language-go"
    windows_exe "libexec/bin/pulumi-language-java.exe", "pulumi-language-java"
    windows_exe "libexec/bin/pulumi-language-nodejs.exe", "pulumi-language-nodejs"
    windows_exe "libexec/bin/pulumi-language-python.exe", "pulumi-language-python"
    windows_exe "libexec/bin/pulumi-language-yaml.exe", "pulumi-language-yaml"
    windows_exe "libexec/bin/pulumi-language-bun.exe", "pulumi-language-bun"
    windows_exe "libexec/bin/pulumi-language-pcl.exe", "pulumi-language-pcl"
    windows_exe "libexec/bin/pulumi-watch.exe", "pulumi-watch"
  end

  test do
    ENV["PULUMI_ACCESS_TOKEN"] = "local://"
    ENV["PULUMI_HOME"] = testpath
    ENV["PULUMI_CONFIG_PASSPHRASE"] = "test"
    assert_match version.to_s, shell_output("#{bin}/pulumi version")
  end
end
