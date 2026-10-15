class Libgccjit < Formula
  desc "JIT library for the GNU compiler collection"
  homepage "https://gcc.gnu.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgccjit-16.2.0-6-any.pkg.tar.zst"
  sha256 "ecdedf223edf93337cdb4ebecaa30819b644c10dcc44fd68f39d54b330694421"
  license "GPL-3.0-or-later" => { with: "GCC-exception-3.1" }

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ftpmirror.gnu.org/gcc/gcc-16.2.0/gcc-16.2.0.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libgccjit.h"
    assert_path_exists lib/"libgccjit.dll.a"
    assert_path_exists bin/"libgccjit-0.dll"
  end
end
