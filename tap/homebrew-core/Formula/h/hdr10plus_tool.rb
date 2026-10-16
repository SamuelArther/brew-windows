class Hdr10plusTool < Formula
  desc "CLI utility to work with HDR10+ in HEVC files"
  homepage "https://github.com/quietvoid/hdr10plus_tool"
  url "https://github.com/quietvoid/hdr10plus_tool/releases/download/1.7.2/hdr10plus_tool-1.7.2-x86_64-pc-windows-msvc.zip"
  sha256 "82b2d560073941b14c6511a431f429e33e134e5caefb60d7e8f6f6e6da8e16ba"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/quietvoid/hdr10plus_tool/archive/refs/tags/1.7.2.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "hdr10plus_tool.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hdr10plus_tool --version 2>&1")
  end
end
