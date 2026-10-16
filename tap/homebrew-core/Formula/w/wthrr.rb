class Wthrr < Formula
  desc "Weather Companion for the Terminal"
  homepage "https://github.com/ttytm/wthrr-the-weathercrab"
  url "https://github.com/ttytm/wthrr-the-weathercrab/releases/download/v1.2.1/wthrr-windows-x86_64.exe"
  sha256 "cea78a775982a4cae845e139afc6022797a6b3f1fc2d0aaf16d692192ddc8ef4"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ttytm/wthrr-the-weathercrab/archive/refs/tags/v1.2.1.tar.gz"
  end

  def install
    bin.install "wthrr-windows-x86_64.exe" => "wthrr.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/wthrr --version 2>&1")
  end
end
