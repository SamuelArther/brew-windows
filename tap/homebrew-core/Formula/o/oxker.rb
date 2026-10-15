class Oxker < Formula
  desc "Terminal User Interface (TUI) to view & control docker containers"
  homepage "https://github.com/mrjackwills/oxker"
  url "https://github.com/mrjackwills/oxker/releases/download/v0.13.4/oxker_windows_x86_64.zip"
  sha256 "5818cd8b3cb32c14f96c70122a0d188cf67a2ccc8f501c9be4151b412b8beee3"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mrjackwills/oxker/archive/refs/tags/v0.13.4.tar.gz"
  end

  def install
    bin.install "oxker.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/oxker --version")

    assert_match "a value is required for '--host <HOST>' but none was supplied",
      shell_output("#{bin}/oxker --host 2>&1", 2)
  end
end
