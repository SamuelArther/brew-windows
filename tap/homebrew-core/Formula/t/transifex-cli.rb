class TransifexCli < Formula
  desc "Transifex command-line client"
  homepage "https://github.com/transifex/cli"
  url "https://github.com/transifex/cli/releases/download/v1.6.17/tx-windows-amd64.zip"
  sha256 "41ee29302f063b28e6f5724c7a690c158880b674f7b69aac08550f4de209626a"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/transifex/cli/archive/refs/tags/v1.6.17.tar.gz"
  end

  def install
    bin.install "tx.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tx --version 2>&1")
  end
end
