class Caddy < Formula
  desc "Powerful, enterprise-ready, open source web server with automatic HTTPS"
  homepage "https://caddyserver.com/"
  url "https://github.com/caddyserver/caddy/releases/download/v2.11.7/caddy_2.11.7_windows_amd64.zip"
  sha256 "0a1edc0b799512051c57071ce0e798d3f2cf67dc98171366f1d2b326072e3b06"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "caddy.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/caddy version")
  end
end
