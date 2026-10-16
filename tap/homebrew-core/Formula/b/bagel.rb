class Bagel < Formula
  desc "CLI to audit posture and evaluate compromise blast radius"
  homepage "https://boostsecurityio.github.io/bagel/"
  url "https://github.com/boostsecurityio/bagel/releases/download/v0.7.1/bagel_Windows_x86_64.zip"
  sha256 "7acc34b48ea22032bbfa532b96c7673e1d1146a14b6f2cd004cff09be4fba820"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/boostsecurityio/bagel/archive/refs/tags/v0.7.1.tar.gz"
  end

  def install
    bin.install "bagel.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/bagel version 2>&1")
  end
end
