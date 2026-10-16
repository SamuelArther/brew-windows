class Hcl2json < Formula
  desc "Convert HCL2 to JSON"
  homepage "https://github.com/tmccombs/hcl2json"
  url "https://github.com/tmccombs/hcl2json/releases/download/v0.6.9/hcl2json_windows_amd64.zip"
  sha256 "5e0ca725336500a4136b8ef6cabd09eae67f2df2e692653cb2eea087465920fd"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tmccombs/hcl2json/archive/refs/tags/v0.6.9.tar.gz"
  end

  def install
    bin.install "hcl2json.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hcl2json --version 2>&1")
  end
end
