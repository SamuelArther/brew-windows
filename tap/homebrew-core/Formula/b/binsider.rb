class Binsider < Formula
  desc "Analyzes ELF binaries"
  homepage "https://binsider.dev/"
  url "https://github.com/orhun/binsider/releases/download/v0.3.2/binsider--x86_64-pc-windows-msvc.zip"
  sha256 "acb806543b1a1c0b09a4da782f6ba8c8042591f694b2124cf602424720c33392"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/orhun/binsider/archive/refs/tags/v0.3.2.tar.gz"
  end

  def install
    bin.install "binsider.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/binsider --version 2>&1")
  end
end
