class Libvpx < Formula
  desc "VP8/VP9 video codec"
  homepage "https://www.webmproject.org/code/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libvpx-1.17.0-1-any.pkg.tar.zst"
  sha256 "8ff6919dbb88ea26ce28d22fd81d6f0d6e200cf56a4dd81ffc2d33124fe98024"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/webmproject/libvpx/archive/refs/tags/v1.17.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"vpx/vp8.h"
    assert_path_exists lib/"libvpx.dll.a"
    assert_path_exists bin/"libvpx-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/vpx.pc").read
  end
end
