class NetworkDoctor < Formula
  desc "Network troubleshooting TUI"
  homepage "https://github.com/heymaikol/network-doctor/"
  url "https://github.com/heymaikol/network-doctor/releases/download/v1.19.4/netdoc_1.19.4_windows_amd64.zip"
  sha256 "a770a096328569824f64f65cd94474ada7c74a4ff6e177904d618ef88565340a"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/heymaikol/network-doctor/archive/refs/tags/v1.19.4.tar.gz"
  end

  def install
    bin.install "netdoc.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/netdoc --version 2>&1")
  end
end
