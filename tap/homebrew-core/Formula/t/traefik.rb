class Traefik < Formula
  desc "Modern reverse proxy"
  homepage "https://traefik.io/"
  url "https://github.com/traefik/traefik/releases/download/v3.7.15/traefik_v3.7.15_windows_amd64.zip"
  sha256 "5a25e0e7fca8836228d3ed8dfc5632fc19fcb5530d6c253ecde8a40be458874b"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "traefik.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/traefik version")
  end
end
