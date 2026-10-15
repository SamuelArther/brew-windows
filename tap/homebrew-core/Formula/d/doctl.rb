class Doctl < Formula
  desc "Command-line tool for DigitalOcean"
  homepage "https://docs.digitalocean.com/reference/doctl/"
  version "2.0.0"
  url "https://github.com/digitalocean/doctl/releases/download/v2.0.0/doctl-2.0.0-windows-amd64.zip"
  sha256 "519fca5d4d99c7d2d7ca6771b6ac2a50a872436f40639acdcddd2412b73d1311"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "doctl.exe"
  end

  test do
    assert_match "doctl version #{version}", shell_output("#{bin}/doctl version")
  end
end
