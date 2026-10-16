class Uncover < Formula
  desc "Tool to discover exposed hosts on the internet using multiple search engines"
  homepage "https://github.com/projectdiscovery/uncover"
  url "https://github.com/projectdiscovery/uncover/releases/download/v1.2.1/uncover_1.2.1_windows_amd64.zip"
  sha256 "09e10b9e0c8b0ec723b86d56af8f3b416bb462d6c85c831e53eeaf678d8f61fd"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/projectdiscovery/uncover/archive/refs/tags/v1.2.1.tar.gz"
  end

  def install
    bin.install "uncover.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/uncover -version 2>&1")
    assert_match "no keys were found", shell_output("#{bin}/uncover -q brew -e shodan 2>&1", 1)
  end
end
