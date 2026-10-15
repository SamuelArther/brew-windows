class T1utils < Formula
  desc "Command-line tools for dealing with Type 1 fonts"
  homepage "https://www.lcdf.org/type/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-t1utils-1.42-2-any.pkg.tar.zst"
  sha256 "1a2371ad3bf64894434e8991c8739ee8cadd4ce169d1a7c575ffc8885f8e0d3a"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://www.lcdf.org/type/t1utils-1.42.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/t1ascii.exe", "t1ascii"
    msys2_bin "ucrt64/bin/t1asm.exe", "t1asm"
    msys2_bin "ucrt64/bin/t1binary.exe", "t1binary"
    msys2_bin "ucrt64/bin/t1disasm.exe", "t1disasm"
    msys2_bin "ucrt64/bin/t1mac.exe", "t1mac"
    msys2_bin "ucrt64/bin/t1unmac.exe", "t1unmac"
  end

  test do
    system bin/"t1mac", "--version"
  end
end
