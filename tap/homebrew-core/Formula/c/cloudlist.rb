class Cloudlist < Formula
  desc "Tool for listing assets from multiple cloud providers"
  homepage "https://github.com/projectdiscovery/cloudlist"
  url "https://github.com/projectdiscovery/cloudlist/archive/refs/tags/v1.5.1.tar.gz"
  sha256 "e75a769df7418f51b011b444c62fd4b758547e2dcc130369f69872a1b6e7009c"
  license "MIT"
  head "https://github.com/projectdiscovery/cloudlist.git", branch: "dev"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "386879ccd67c91a4a1ba91ca75eca4528bccbf73e13ce80b741b98eac4a1c678"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/cloudlist"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cloudlist -version 2>&1")

    output = shell_output bin/"cloudlist", 1
    assert_match output, "invalid provider configuration file provided"
  end
end
