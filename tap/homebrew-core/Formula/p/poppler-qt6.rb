class PopplerQt6 < Formula
  desc "PDF rendering library (based on the xpdf-3.0 code base)"
  homepage "https://poppler.freedesktop.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-poppler-qt6-26.08.0-1-any.pkg.tar.zst"
  sha256 "12489ce93472e61409bc4db2d2e93f49a7a07c9184069e62973248ed40a4f45e"
  license any_of: ["GPL-2.0-only", "GPL-3.0-only"] # see README-XPDF

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://poppler.freedesktop.org/poppler-26.10.0.tar.xz"
  end

  keg_only "it conflicts with poppler"

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"poppler/qt6/poppler-qt6.h"
    assert_path_exists lib/"libpoppler-qt6.dll.a"
    assert_path_exists bin/"libpoppler-qt6-3.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/poppler-qt6.pc").read
  end
end
