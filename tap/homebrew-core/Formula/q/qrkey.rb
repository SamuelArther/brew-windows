class Qrkey < Formula
  desc "Generate and recover QR codes from files for offline private key backup"
  homepage "https://github.com/Techwolf12/qrkey"
  url "https://github.com/Techwolf12/qrkey/releases/download/v0.0.1/qrkey_Windows_x86_64.zip"
  sha256 "c8361914f9ede7bf582e05f1a60126c4df78713251b88c34e4c265ddc3f7272f"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Techwolf12/qrkey/archive/refs/tags/v0.0.1.tar.gz"
  end

  def install
    bin.install "qrkey.exe"
  end

  test do
    system bin/"qrkey", "--help"
  end
end
