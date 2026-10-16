class TrzszGo < Formula
  desc "Simple file transfer tools, similar to lrzsz (rz/sz), and compatible with tmux"
  homepage "https://trzsz.github.io"
  url "https://github.com/trzsz/trzsz-go/releases/download/v1.2.0/trzsz_1.2.0_windows_x86_64.zip"
  sha256 "0b46318a0fc7e39beaf97e6f07ab257f5b36fdffc3304118b6f72412dbbb52ca"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/trzsz/trzsz-go/archive/refs/tags/v1.2.0.tar.gz"
  end

  def install
    bin.install "trz.exe"
    bin.install "trzsz.exe"
    bin.install "tsz.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/trz --version 2>&1")
  end
end
