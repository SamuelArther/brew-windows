class Litehtml < Formula
  desc "Fast and lightweight HTML/CSS rendering engine"
  homepage "http://www.litehtml.com/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-litehtml-0.10-2-any.pkg.tar.zst"
  sha256 "7843f5e7d87fc4149ef6b7ad09162a01c48adbc427ffedbf72c1cf2afaf98697"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/litehtml/litehtml/archive/refs/tags/v0.10.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"litehtml/url.h"
    assert_path_exists lib/"liblitehtml.dll.a"
    assert_path_exists bin/"liblitehtml-0.dll"
  end
end
