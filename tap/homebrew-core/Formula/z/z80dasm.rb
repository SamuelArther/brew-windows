class Z80dasm < Formula
  desc "Disassembler for the Zilog Z80 microprocessor and compatibles"
  homepage "https://www.tablix.org/~avian/blog/articles/z80dasm/"
  url "https://www.tablix.org/~avian/z80dasm/z80dasm-1.2.0.tar.gz"
  mirror "https://geeklan.co.uk/files/z80dasm-1.2.0.tar.gz"
  sha256 "8da2c4a58a3917a8229dec0da97e718f90ede84985424d74456575bf5acfeec8"
  license "GPL-2.0-or-later"

  livecheck do
    url "https://www.tablix.org/~avian/z80dasm/"
    regex(/href=.*?z80dasm[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/z80dasm-1.2.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "be263612439f8371147693ac2a3a3b53a72af1af54b392509f124a06cb474d5e"
  end

  deny_network_access!

  def install
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    path = testpath/"a.bin"
    path.binwrite [0xcd, 0x34, 0x12].pack("c*")
    assert_match "call 01234h", shell_output("#{bin}/z80dasm #{path}")
  end
end
