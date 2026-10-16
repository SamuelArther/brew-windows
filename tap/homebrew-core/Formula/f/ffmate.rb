class Ffmate < Formula
  desc "FFmpeg automation layer"
  homepage "https://docs.ffmate.io"
  url "https://github.com/welovemedia/ffmate/releases/download/2.0.18/ffmate-windows-amd64.zip"
  sha256 "3a285e2bcd34b864ea972797d2b526272ba9ca878992244907b04ab56b357df3"
  license "AGPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/welovemedia/ffmate.git"
  end

  def install
    bin.install "ffmate.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ffmate version 2>&1")
  end
end
