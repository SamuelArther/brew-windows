class Tass64 < Formula
  desc "Multi pass optimizing macro assembler for the 65xx series of processors"
  homepage "https://tass64.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/tass64/source/64tass-1.60.3243-src.zip"
  sha256 "9d83be3d23a2c55e085b7c7a7856c2f96080447ea120a6a8c21a217ed76427f0"
  license all_of: ["GPL-2.0-or-later", "LGPL-2.0-or-later", "LGPL-2.1-only", "MIT"]
  compatibility_version 1

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "19004110e45985ef732b44c782df1aa8d3c0c53c982d6eed3e87900bae628450"
  end

  def install
    system "make", "install", "CPPFLAGS=-D_XOPEN_SOURCE", "prefix=#{prefix}"

    # `make install` does not install syntax highlighting definitions
    pkgshare.install "syntax"
  end

  test do
    (testpath/"hello.asm").write <<~'ASM'
      ;; Simple "Hello World" program for C64
      *=$c000
        LDY #$00
      L0
        LDA L1,Y
        CMP #0
        BEQ L2
        JSR $FFD2
        INY
        JMP L0
      L1
        .text "HELLO WORLD",0
      L2
        RTS
    ASM

    system bin/"64tass", "-a", "hello.asm", "-o", "hello.prg"
    assert_path_exists testpath/"hello.prg"
  end
end
