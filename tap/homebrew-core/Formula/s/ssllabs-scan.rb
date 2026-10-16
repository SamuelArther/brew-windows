class SsllabsScan < Formula
  desc "This tool is a command-line client for the SSL Labs APIs"
  homepage "https://www.ssllabs.com/projects/ssllabs-apis/"
  url "https://github.com/ssllabs/ssllabs-scan/releases/download/v1.4.0/ssllabs-scan_1.4.0_win64.zip"
  sha256 "0e717eb7daf80ad1c77e48a87091ed162c889ceca019f333b9288bc4ee93f2b8"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ssllabs/ssllabs-scan/archive/refs/tags/v1.5.0.tar.gz"
  end

  def install
    bin.install "ssllabs-scan.exe"
  end

  test do
    system bin/"ssllabs-scan", "-grade", "-quiet", "-usecache", "ssllabs.com"
  end
end
