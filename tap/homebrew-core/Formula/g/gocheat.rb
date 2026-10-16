class Gocheat < Formula
  desc "TUI Cheatsheet for keybindings, hotkeys and more"
  homepage "https://github.com/Achno/gocheat"
  url "https://github.com/Achno/gocheat/releases/download/v0.1.1/gocheat_0.1.1_windows_amd64.tar.gz"
  sha256 "4a86d4b7164d899757fa6521495a735b453db5d05845b31ac94a3289cd0799af"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Achno/gocheat/archive/refs/tags/v0.1.1.tar.gz"
  end

  def install
    bin.install "gocheat.exe"
  end

  test do
    assert_path_exists bin/"gocheat.exe"
  end
end
