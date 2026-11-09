class Avra < Formula
  desc "Assembler for the Atmel AVR microcontroller family"
  homepage "https://github.com/Ro5bert/avra"
  url "https://github.com/Ro5bert/avra/archive/refs/tags/1.4.2.tar.gz"
  sha256 "cc56837be973d1a102dc6936a0b7235a1d716c0f7cd053bf77e0620577cff986"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f54ae5657f3d317168db5ede7df7ec161f71a20afaabe1de6020c1a313171dcb"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build

  deny_network_access!

  def install
    system "make", "install", "PREFIX=#{prefix}", "OS=osx"
    pkgshare.install Dir["includes/*"]
  end

  test do
    (testpath/"test.asm").write " .device attiny10\n ldi r16,0x42\n"
    output = shell_output("#{bin}/avra -l test.lst test.asm")
    assert_match "Assembly complete with no errors.", output
    assert_path_exists testpath/"test.hex"
    assert_match "ldi r16,0x42", File.read("test.lst")
  end
end
