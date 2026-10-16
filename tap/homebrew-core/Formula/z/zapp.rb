class Zapp < Formula
  desc "Flash ZSA keyboards from your terminal"
  homepage "https://github.com/zsa/zapp"
  url "https://github.com/zsa/zapp/releases/download/v1.0.2/zapp-windows-x86_64.zip"
  sha256 "c81f4c73ce823c7554cbb0896422d27351de3eb096548c5500a8394b7e8136fd"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/zsa/zapp/archive/refs/tags/v1.0.2.tar.gz"
  end

  def install
    bin.install "zapp.exe"
  end

  test do
    firmware = testpath/"invalid.bin"
    firmware.write "not valid firmware"

    output = shell_output("#{bin}/zapp flash #{firmware} 2>&1", 1)
    assert_match "Error: Failed to load firmware", output

    assert_match version.to_s, shell_output("#{bin}/zapp --version")
  end
end
