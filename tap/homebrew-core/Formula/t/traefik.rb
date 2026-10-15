class Traefik < Formula
  desc "Modern reverse proxy"
  homepage "https://traefik.io/"
  url "https://github.com/traefik/traefik/releases/download/v3.7.14/traefik_v3.7.14_windows_amd64.zip"
  sha256 "47b5d11c5d8b2d798357edf9d73e33a40bb045b4c7371da5e322c5083501c5c4"
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
