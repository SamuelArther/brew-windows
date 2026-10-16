class Naabu < Formula
  desc "Fast port scanner"
  homepage "https://docs.projectdiscovery.io/tools/naabu/overview"
  url "https://github.com/projectdiscovery/naabu/releases/download/v2.6.1/naabu_2.6.1_windows_amd64.zip"
  sha256 "4efaa341f36a0c6e676164f75e2d93d5a72f4090d690fd1bc293f5a4b79c202b"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/projectdiscovery/naabu/archive/refs/tags/v2.6.1.tar.gz"
  end

  def install
    bin.install "naabu.exe"
  end

  test do
    assert_match "brew.sh:443", shell_output("#{bin}/naabu -host brew.sh -p 443")

    assert_match version.to_s, shell_output("#{bin}/naabu --version 2>&1")
  end
end
