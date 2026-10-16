class Pixtuoid < Formula
  desc "Terminal pixel-art office for AI coding agents"
  homepage "https://github.com/IvanWng97/pixtuoid"
  url "https://github.com/pixtuoid/pixtuoid/releases/download/v0.19.0/pixtuoid-v0.19.0-x86_64-pc-windows-msvc.zip"
  sha256 "79fed813fbf9b5fac05dcc76307f99f43b6cf1a66d36cd7e6c646f42f91d7f73"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/IvanWng97/pixtuoid/archive/refs/tags/v0.19.0.tar.gz"
  end

  def install
    bin.install "pixtuoid.exe"
    bin.install "pixtuoid-hook.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pixtuoid --version 2>&1")
  end
end
