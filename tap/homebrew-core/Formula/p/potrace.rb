class Potrace < Formula
  desc "Convert bitmaps to vector graphics"
  homepage "https://potrace.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-potrace-1.16-3-any.pkg.tar.zst"
  sha256 "9e1c381653385b41d5aa9e682fe9010743fa709b38a9094f8b896f08973e3b28"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url :homepage
    regex(/href=.*?potrace[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/mkbitmap.exe", "mkbitmap"
    msys2_bin "ucrt64/bin/potrace.exe", "potrace"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/potrace --version 2>&1")
  end
end
