class Udis86 < Formula
  desc "Minimalistic disassembler library for x86"
  homepage "https://sourceforge.net/projects/udis86/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-udis86-1.7.3rc1-3-any.pkg.tar.zst"
  sha256 "de009c0117c6d41825f4d4b093c69ae897278f275c4697055ac38a6d194d8a29"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://downloads.sourceforge.net/project/udis86/udis86/1.7/udis86-1.7.2.tar.gz"
    regex(%r{url=.*?/udis86[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/udcli.exe", "udcli"
  end

  test do
    assert_match("int 0x80", pipe_output("#{bin}/udcli -x", "cd 80").split.last(2).join(" "))
  end
end
