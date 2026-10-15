class Gmp < Formula
  desc "GNU multiple precision arithmetic library"
  homepage "https://gmplib.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gmp-6.3.0-2-any.pkg.tar.zst"
  sha256 "e82a75968a556484a50084578238a84eb60fb93e34986fd6695c537975bd39ea"
  license any_of: ["LGPL-3.0-or-later", "GPL-2.0-or-later"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gmplib.org/download/gmp/"
    regex(/href=.*?gmp[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gmp.h"
    assert_path_exists lib/"libgmp.dll.a"
    assert_path_exists bin/"libgmp-10.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gmp.pc").read
  end
end
