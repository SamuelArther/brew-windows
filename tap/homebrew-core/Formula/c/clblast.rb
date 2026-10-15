class Clblast < Formula
  desc "Tuned OpenCL BLAS library"
  homepage "https://github.com/CNugteren/CLBlast"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-clblast-1.6.3-3-any.pkg.tar.zst"
  sha256 "bf2c8363a99a2a8708c881634916df18c623eb0eab749a9c725b7fffb113a0dc"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/CNugteren/CLBlast/archive/refs/tags/1.7.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"clblast.h"
    assert_path_exists lib/"libclblast.dll.a"
    assert_path_exists bin/"libclblast.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/clblast.pc").read
  end
end
