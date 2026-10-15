class Dbcsr < Formula
  desc "Distributed Block Compressed Sparse Row matrix library"
  homepage "https://cp2k.github.io/dbcsr/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-dbcsr-2.10.0-1-any.pkg.tar.zst"
  sha256 "9a5814e5b9743a2f987000a5fae26acafee19245b93fb82654ac1bd0ce3d97b4"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/cp2k/dbcsr/releases/download/v2.10.0/dbcsr-2.10.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists lib/"libdbcsr.dll.a"
    assert_path_exists bin/"libdbcsr.dll"
  end
end
