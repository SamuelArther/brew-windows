class Envio < Formula
  desc "Modern And Secure CLI Tool For Managing Environment Variables"
  homepage "https://github.com/humblepenguinn/envio"
  url "https://github.com/humblepenguinn/envio/releases/download/v0.8.0/envio-v0.8.0-x86_64-pc-windows-gnu.zip"
  sha256 "6fda154e37b2a95edc74671d3183ac502e7de12d15678448dcffd92075131557"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/humblepenguinn/envio/archive/refs/tags/v0.8.0.tar.gz"
  end

  def install
    bin.install "envio.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/envio version 2>&1")
  end
end
