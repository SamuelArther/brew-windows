class Macchina < Formula
  desc "System information fetcher, with an emphasis on performance and minimalism"
  homepage "https://github.com/Macchina-CLI/macchina"
  url "https://github.com/Macchina-CLI/macchina/releases/download/v6.4.0/macchina-v6.4.0-windows-x86_64.exe"
  sha256 "2b7634cc1d7473d0cc7623773762b541023454f22bce0906a90a5bb2c2a52644"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Macchina-CLI/macchina/archive/refs/tags/v6.4.0.tar.gz"
  end

  def install
    bin.install "macchina-v6.4.0-windows-x86_64.exe" => "macchina.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/macchina --version 2>&1")
  end
end
