class Eatmemory < Formula
  desc "Simple program to allocate memory from the command-line"
  homepage "https://github.com/julman99/eatmemory"
  url "https://github.com/julman99/eatmemory/releases/download/v1.1.0/eatmemory-1.1.0-windows-x86_64.zip"
  sha256 "82188458cd41ee176095d8b5fd1d95b95b2a72f1ff2e6a0b646d62399270f0af"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/julman99/eatmemory/archive/refs/tags/v1.1.0.tar.gz"
  end

  def install
    bin.install "eatmemory.exe"
  end

  test do
    system bin/"eatmemory", "--help"
  end
end
