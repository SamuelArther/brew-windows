class Gputils < Formula
  desc "GNU PIC Utilities"
  homepage "https://gputils.sourceforge.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gputils-1.5.2-2-any.pkg.tar.zst"
  sha256 "dca381ff36410470d9b50853d938f8356337b08e7672ff92d79794f32e1f3ac3"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://downloads.sourceforge.net/project/gputils/gputils/1.5.0/gputils-1.5.2.tar.bz2"
    regex(%r{url=.*?/gputils[._-]v?(\d+(?:\.\d+)+(?:-\d+)?)\.t}i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/gpasm.exe", "gpasm"
    msys2_bin "ucrt64/bin/gpdasm.exe", "gpdasm"
    msys2_bin "ucrt64/bin/gplib.exe", "gplib"
    msys2_bin "ucrt64/bin/gplink.exe", "gplink"
    msys2_bin "ucrt64/bin/gpstrip.exe", "gpstrip"
    msys2_bin "ucrt64/bin/gpvc.exe", "gpvc"
    msys2_bin "ucrt64/bin/gpvo.exe", "gpvo"
  end

  test do
    # assemble with gpasm
    (testpath/"test.asm").write " movlw 0x42\n end\n"
    system bin/"gpasm", "-p", "p16f84", "test.asm"
    assert_path_exists testpath/"test.hex"

    # disassemble with gpdasm
    output = shell_output("#{bin}/gpdasm -p p16f84 test.hex")
    assert_match "0000:  3042  movlw   0x42\n", output
  end
end
