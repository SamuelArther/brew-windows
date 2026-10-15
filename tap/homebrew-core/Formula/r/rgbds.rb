class Rgbds < Formula
  desc "Rednex GameBoy Development System"
  homepage "https://rgbds.gbdev.io"
  url "https://github.com/gbdev/rgbds/releases/download/v1.0.4/rgbds-win64.zip"
  sha256 "bd1d7e386f290afd57f4ea9a7353bfcd4ae10884a94c5c1f2fc42a75db7f97e6"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gbdev/rgbds/archive/refs/tags/v1.0.4.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "rgbasm.exe"
    bin.install "rgbfix.exe"
    bin.install "rgbgfx.exe"
    bin.install "rgblink.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rgbasm --version 2>&1")
  end
end
