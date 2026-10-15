class FrameworkToolTui < Formula
  desc "TUI for controlling and monitoring Framework Computers hardware"
  homepage "https://github.com/grouzen/framework-tool-tui"
  url "https://github.com/grouzen/framework-tool-tui/releases/download/v0.8.6/framework-tool-tui-v0.8.6-x86_64-pc-windows-msvc.zip"
  sha256 "9a330ea354454e77db6b417cae82e62e6b0f7ed8cd41c9994513179445d5641d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/grouzen/framework-tool-tui/archive/refs/tags/v0.8.6.tar.gz"
  end

  def install
    bin.install "framework-tool-tui.exe"
  end

  test do
    assert_path_exists bin/"framework-tool-tui.exe"
  end
end
