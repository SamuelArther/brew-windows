class BeancountLanguageServer < Formula
  desc "Language server for beancount files"
  homepage "https://github.com/polarmutex/beancount-language-server"
  url "https://github.com/polarmutex/beancount-language-server/releases/download/1.9.2/beancount-language-server-windows-x64.zip"
  sha256 "ed12f040b8e642ad048592c8588ed4a89a91b3c16e1d3fc61eeb2d3bfbd51678"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/polarmutex/beancount-language-server/archive/refs/tags/1.9.2.tar.gz"
  end

  def install
    bin.install "beancount-language-server.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/beancount-language-server --version 2>&1")
  end
end
