class Xleak < Formula
  desc "Terminal Excel viewer with an interactive TUI"
  homepage "https://github.com/bgreenwell/xleak"
  url "https://github.com/bgreenwell/xleak/releases/download/v0.2.6/xleak-x86_64-pc-windows-msvc.zip"
  sha256 "9d2355f5ed9dd2d08b354ebeb28743319b845c45c7524d384869026b42ed44f4"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bgreenwell/xleak/releases/download/v0.2.6/source.tar.gz"
  end

  def install
    bin.install "xleak.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/xleak --version 2>&1")
  end
end
