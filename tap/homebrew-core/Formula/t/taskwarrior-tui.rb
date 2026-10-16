class TaskwarriorTui < Formula
  desc "Terminal user interface for taskwarrior"
  homepage "https://kdheepak.com/taskwarrior-tui/"
  url "https://github.com/kdheepak/taskwarrior-tui/releases/download/v0.27.0/taskwarrior-tui-x86_64-pc-windows-gnu.tar.gz"
  sha256 "bbb104330a154bd599fd814dd1451b0a5bc48ac76d37f53dd7d6f3bacd9b24fa"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kdheepak/taskwarrior-tui/archive/refs/tags/v0.27.0.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "taskwarrior-tui.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/taskwarrior-tui --version")
    assert_match "a value is required for '--report <STRING>' but none was supplied",
      shell_output("#{bin}/taskwarrior-tui --report 2>&1", 2)
  end
end
