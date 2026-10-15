class Libplacebo < Formula
  desc "Reusable library for GPU-accelerated image/video processing primitives"
  homepage "https://code.videolan.org/videolan/libplacebo"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libplacebo-7.360.1-2-any.pkg.tar.zst"
  sha256 "ef183923c29475320d75a3b06fff5729172fcfdcefc45b660ed28c41107d4de4"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://code.videolan.org/videolan/libplacebo/-/archive/v7.360.1/libplacebo-v7.360.1.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libplacebo/gpu.h"
    assert_path_exists lib/"libplacebo.dll.a"
    assert_path_exists bin/"libplacebo-360.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libplacebo.pc").read
  end
end
