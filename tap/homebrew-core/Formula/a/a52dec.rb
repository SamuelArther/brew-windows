class A52dec < Formula
  desc "Library for decoding ATSC A/52 streams (AKA 'AC-3')"
  homepage "https://git.adelielinux.org/community/a52dec/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-a52dec-0.7.4-5-any.pkg.tar.zst"
  sha256 "9f85ed36b35b9e2d5bec87ff3d0e295d28fabd83e7b2b6cb4fe4575aaa5dfd61"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://distfiles.adelielinux.org/source/a52dec/"
    regex(/href=.*?a52dec[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/a52dec.exe", "a52dec"
    msys2_bin "ucrt64/bin/extract_a52.exe", "extract_a52"
  end

  test do
    touch testpath/"test"
    system bin/"a52dec", "-o", "null", "test"
  end
end
