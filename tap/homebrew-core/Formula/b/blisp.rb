class Blisp < Formula
  desc "ISP tool & library for Bouffalo Labs RISC-V Microcontrollers and SoCs"
  homepage "https://github.com/pine64/blisp"
  url "https://github.com/pine64/blisp/releases/download/v0.0.5/blisp-windows-x86_64.zip"
  sha256 "b30dbec2052018f6731c538d25df9e41c356870cd7bf244956ba81f42a39980b"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pine64/blisp/archive/refs/tags/v0.0.5.tar.gz"
  end

  def install
    bin.install "blisp.exe"
  end

  test do
    output = shell_output("#{bin}/blisp write -c bl70x --reset Pinecilv2_EN.bin 2>&1", 11)
    assert_match "Input firmware not found: Pinecilv2_EN.bin", output

    assert_match version.to_s, shell_output("#{bin}/blisp --version")
  end
end
