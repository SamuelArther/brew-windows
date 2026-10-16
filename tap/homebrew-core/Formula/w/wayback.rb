class Wayback < Formula
  desc "Archiving tool integrated with various archival services"
  homepage "https://docs.wabarc.eu.org"
  url "https://github.com/wabarc/wayback/releases/download/v0.21.1/wayback-windows-amd64-0.21.1.zip"
  sha256 "80c5b353d8fc180d725d76a44758ad6c3fa6dc381c2bd56b0e21ce785b9f15e2"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/wabarc/wayback/archive/refs/tags/v0.21.1.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "wayback-windows-amd64.exe" => "wayback.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/wayback --version 2>&1")
  end
end
