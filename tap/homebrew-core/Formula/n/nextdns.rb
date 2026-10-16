class Nextdns < Formula
  desc "CLI for NextDNS's DNS-over-HTTPS (DoH)"
  homepage "https://nextdns.io"
  url "https://github.com/nextdns/nextdns/releases/download/v1.47.3/nextdns_1.47.3_windows_amd64.zip"
  sha256 "b9ea2c3b2286d62aff63c48356c06ee75bf9fb775b7d9800b3c0d9febaedcbfc"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/nextdns/nextdns/archive/refs/tags/v1.47.3.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "nextdns.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nextdns version 2>&1")
  end
end
