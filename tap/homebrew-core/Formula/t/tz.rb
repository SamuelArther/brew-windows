class Tz < Formula
  desc "CLI time zone visualizer"
  homepage "https://github.com/oz/tz"
  url "https://github.com/oz/tz/releases/download/v0.8.0/tz_0.8.0_windows_amd64.tar.gz"
  sha256 "8d00f5f757404d63c9cdaf6dbbeaaa71e4735ab51695865478b401f64175ffb8"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/oz/tz/archive/refs/tags/v0.8.0.tar.gz"
  end

  def install
    bin.install "tz.exe"
  end

  test do
    assert_match "US/Eastern", shell_output("#{bin}/tz --list")

    assert_match version.to_s, shell_output("#{bin}/tz -v")
  end
end
