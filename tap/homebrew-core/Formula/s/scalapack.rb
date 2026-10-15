class Scalapack < Formula
  desc "High-performance linear algebra for distributed memory machines"
  homepage "https://netlib.org/scalapack/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-scalapack-2.2.3-1-any.pkg.tar.zst"
  sha256 "1b5ed75db25094d3e71b4b21aaa019a9861192a6d5a3da34b22b311845938119"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/Reference-ScaLAPACK/scalapack/archive/refs/tags/v2.2.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists lib/"libscalapack.dll.a"
    assert_path_exists bin/"libscalapack.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/scalapack.pc").read
  end
end
