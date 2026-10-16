class Mtools < Formula
  desc "Tools for manipulating MSDOS files"
  homepage "https://www.gnu.org/software/mtools/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mtools-4.0.49-1-any.pkg.tar.zst"
  sha256 "7889b8ffeb440a96413f9a034e72447b2dd7fa03c861e7f7cb7af3cd5dbc9754"
  license "GPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://ftpmirror.gnu.org/mtools/mtools-4.0.49.tar.gz"
  end

  conflicts_with "multimarkdown", because: "both install `mmd` binaries"
  conflicts_with "mcat", because: "both install `mcat` binaries"

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/mattrib.exe", "mattrib"
    msys2_bin "ucrt64/bin/mbadblocks.exe", "mbadblocks"
    msys2_bin "ucrt64/bin/mcat.exe", "mcat"
    msys2_bin "ucrt64/bin/mcd.exe", "mcd"
    msys2_bin "ucrt64/bin/mcopy.exe", "mcopy"
    msys2_bin "ucrt64/bin/mdel.exe", "mdel"
    msys2_bin "ucrt64/bin/mdeltree.exe", "mdeltree"
    msys2_bin "ucrt64/bin/mdir.exe", "mdir"
    msys2_bin "ucrt64/bin/mdoctorfat.exe", "mdoctorfat"
    msys2_bin "ucrt64/bin/mdu.exe", "mdu"
    msys2_bin "ucrt64/bin/mformat.exe", "mformat"
    msys2_bin "ucrt64/bin/minfo.exe", "minfo"
    msys2_bin "ucrt64/bin/mkmanifest.exe", "mkmanifest"
    msys2_bin "ucrt64/bin/mlabel.exe", "mlabel"
    msys2_bin "ucrt64/bin/mmd.exe", "mmd"
    msys2_bin "ucrt64/bin/mmount.exe", "mmount"
    msys2_bin "ucrt64/bin/mmove.exe", "mmove"
    msys2_bin "ucrt64/bin/mpartition.exe", "mpartition"
    msys2_bin "ucrt64/bin/mrd.exe", "mrd"
    msys2_bin "ucrt64/bin/mren.exe", "mren"
    msys2_bin "ucrt64/bin/mshortname.exe", "mshortname"
    msys2_bin "ucrt64/bin/mshowfat.exe", "mshowfat"
    msys2_bin "ucrt64/bin/mtools.exe", "mtools"
    msys2_bin "ucrt64/bin/mtoolstest.exe", "mtoolstest"
    msys2_bin "ucrt64/bin/mtype.exe", "mtype"
    msys2_bin "ucrt64/bin/mzip.exe", "mzip"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mtools --version")
  end
end
