class Proxelar < Formula
  desc "Man-in-the-Middle proxy for HTTP/HTTPS traffic"
  homepage "https://proxelar.micheletti.io"
  url "https://github.com/emanuele-em/proxelar/releases/download/v0.6.1/proxelar-v0.6.1-x86_64-pc-windows-msvc.zip"
  sha256 "7614d89b08e38fae95f2ee1d5961f13dcaadbdc678aa9d6d67c4f3f29770d135"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/emanuele-em/proxelar/archive/refs/tags/v0.6.1.tar.gz"
  end

  def install
    bin.install "proxelar.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/proxelar --version 2>&1")
  end
end
