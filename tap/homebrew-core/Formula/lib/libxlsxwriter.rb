class Libxlsxwriter < Formula
  desc "C library for creating Excel XLSX files"
  homepage "https://libxlsxwriter.github.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libxlsxwriter-1.2.4-1-any.pkg.tar.zst"
  sha256 "c3fe91e9d40ec422ac50b2a7c52a95bd0cbfac66d7cee2b64043343144ea4036"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/jmcnamara/libxlsxwriter/archive/refs/tags/v1.2.4.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"xlsxwriter.h"
    assert_path_exists lib/"libxlsxwriter.dll.a"
    assert_path_exists bin/"libxlsxwriter.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/xlsxwriter.pc").read
  end
end
