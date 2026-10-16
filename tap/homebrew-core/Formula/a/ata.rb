class Ata < Formula
  desc "ChatGPT in the terminal"
  homepage "https://github.com/transformrs/ata"
  url "https://github.com/transformrs/ata/releases/download/v2.0.4/ata-x86_64-pc-windows-msvc.exe"
  sha256 "af9e07505927f5d23adf5afe9b716506fbc9c3ea032d1fef5f8ee3bf3bf1c64c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/transformrs/ata/archive/refs/tags/v2.0.4.tar.gz"
  end

  def install
    bin.install "ata-x86_64-pc-windows-msvc.exe" => "ata.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ata --version 2>&1")
  end
end
