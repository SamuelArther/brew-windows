class Nasm < Formula
  desc "Netwide Assembler (NASM) is an 80x86 assembler"
  homepage "https://www.nasm.us/"
  url "https://www.nasm.us/pub/nasm/releasebuilds/3.02/win64/nasm-3.02-win64.zip"
  sha256 "161d0bfaff53c2f9e9f3e69fd0672323ebabafd1268976a5cec11be92a19aee7"
  license "BSD-2-Clause"

  livecheck do
    url "https://www.nasm.us/pub/nasm/releasebuilds/"
    regex(%r{href=["']?v?(\d+(?:\.\d+)+)/?["' >]}i)
  end

  def install
    bin.install "nasm.exe"
    bin.install "ndisasm.exe"
  end

  test do
    (testpath/"foo.s").write <<~ASM
      mov eax, 0
      mov ebx, 0
      int 0x80
    ASM
    system bin/"nasm", "foo.s"
    code = File.binread(testpath/"foo")
    assert_equal [0x66, 0xb8, 0, 0, 0, 0, 0x66, 0xbb, 0, 0, 0, 0, 0xcd, 0x80], code.bytes
  end
end
