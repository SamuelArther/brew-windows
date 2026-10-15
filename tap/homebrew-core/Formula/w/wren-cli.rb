class WrenCli < Formula
  desc "Simple REPL and CLI tool for running Wren scripts"
  homepage "https://github.com/wren-lang/wren-cli"
  url "https://github.com/wren-lang/wren-cli/releases/download/0.4.0/wren-cli-windows-0.4.0.zip"
  sha256 "4ad884d0b319aea9ed663c6c10d562d348ebfedd03c90384aa19cab24d61934d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/wren-lang/wren-cli/archive/refs/tags/0.4.0.tar.gz"
  end

  def install
    bin.install "wren_cli.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/wren_cli --version 2>&1")
  end
end
