class Pipet < Formula
  desc "Swiss-army tool for web scraping, made for hackers"
  homepage "https://github.com/bjesus/pipet"
  url "https://github.com/bjesus/pipet/releases/download/0.3.0/pipet-windows-amd64.zip"
  sha256 "bbfd4f3bb27ccef0340fcb24d2a69712f75848fc4988d8dd866cdc83446d344c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bjesus/pipet/archive/refs/tags/0.3.0.tar.gz"
  end

  def install
    bin.install "pipet-windows-amd64.exe" => "pipet.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pipet --version 2>&1")
  end
end
