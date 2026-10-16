class Vuls < Formula
  desc "Agentless Vulnerability Scanner for Linux/FreeBSD"
  homepage "https://vuls.io/"
  url "https://github.com/future-architect/vuls/releases/download/v0.42.0/future-vuls_0.42.0_windows_amd64.tar.gz"
  sha256 "fb093b753f8ded9989292bd199cbc4ab21a04c17a84a19c1358ef9662d9b8b67"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/future-architect/vuls/archive/refs/tags/v0.42.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "future-vuls.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/future-vuls version 2>&1")
  end
end
