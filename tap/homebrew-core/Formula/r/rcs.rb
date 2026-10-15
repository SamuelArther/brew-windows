class Rcs < Formula
  desc "GNU revision control system"
  homepage "https://www.gnu.org/software/rcs/"
  url "https://repo.msys2.org/msys/x86_64/rcs-5.10.1-2-x86_64.pkg.tar.zst"
  sha256 "549c8d78bf09d51d2d42bfd9a457bed4aa9215e7295c2af0571c1e2061daf875"
  license "GPL-3.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://ftpmirror.gnu.org/rcs/rcs-5.10.1.tar.lz"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/ident.exe", "ident"
    msys2_bin "usr/bin/merge.exe", "merge"
    msys2_bin "usr/bin/rcs.exe", "rcs"
  end

  test do
    system bin/"merge", "--version"
  end
end
