class Regclient < Formula
  desc "Docker and OCI Registry Client in Go and tooling using those libraries"
  homepage "https://regclient.org/"
  url "https://github.com/regclient/regclient/releases/download/v0.11.6/regbot-windows-amd64.exe"
  sha256 "34f42e8cbfbbc1e0ccca3ebe552841921a3a2cef2c4c8cb0954d5fa3fa699ffe"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/regclient/regclient/archive/refs/tags/v0.11.6.tar.gz"
  end

  def install
    bin.install "regbot-windows-amd64.exe" => "regbot.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/regbot version 2>&1")
  end
end
