class DoviTool < Formula
  desc "CLI tool for Dolby Vision metadata on video streams"
  homepage "https://github.com/quietvoid/dovi_tool/"
  url "https://github.com/quietvoid/dovi_tool/releases/download/2.3.4/dovi_tool-2.3.4-x86_64-pc-windows-msvc.zip"
  sha256 "0d9733d311dfe49f9dca0c350f7e6d978182673d333c6f07f7365daf9e6c81eb"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/quietvoid/dovi_tool/archive/refs/tags/2.3.4.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "dovi_tool.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dovi_tool --version 2>&1")
  end
end
