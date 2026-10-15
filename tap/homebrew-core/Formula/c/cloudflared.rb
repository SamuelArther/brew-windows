class Cloudflared < Formula
  desc "Cloudflare Tunnel client (formerly Argo Tunnel)"
  homepage "https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/"
  url "https://github.com/cloudflare/cloudflared/releases/download/2026.10.0/cloudflared-windows-amd64.exe"
  sha256 "86aee4017b26625cee8484c113558f48effa4cd47f7aa05fcf425604e5d2b23c"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "cloudflared-windows-amd64.exe" => "cloudflared.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cloudflared --version")
  end
end
