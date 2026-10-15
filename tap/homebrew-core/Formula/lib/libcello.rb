class Libcello < Formula
  desc "Higher-level programming in C"
  homepage "https://libcello.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libcello-2.1.0.313.dfcd86c-1-any.pkg.tar.zst"
  sha256 "8e9dc61250c97dbd5ae873dd6020a8d1c776b37c1305a119c3afe27dac08e6b3"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :homepage
    regex(/href=.*?libCello[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"Cello.h"
    assert_path_exists bin/"libCello.dll"
  end
end
