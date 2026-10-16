class Rogcat < Formula
  desc "Adb logcat wrapper"
  homepage "https://github.com/flxo/rogcat"
  url "https://github.com/flxo/rogcat/releases/download/v0.5.0/rogcat-x86_64-pc-windows-msvc.zip"
  sha256 "9cb3d42094b73c490bb38c8c47d4ad0b4a6dbb133f3d6ff40ad11a25ad1734bb"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/flxo/rogcat/archive/refs/tags/v0.5.0.tar.gz"
  end

  def install
    bin.install "rogcat.exe"
  end

  test do
    output = shell_output("#{bin}/rogcat devices 2>&1", 101)
    assert_match "Failed to find adb", output

    assert_match version.to_s, shell_output("#{bin}/rogcat --version")
  end
end
