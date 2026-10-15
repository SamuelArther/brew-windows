class Zimg < Formula
  desc "Scaling, colorspace conversion, and dithering library"
  homepage "https://github.com/sekrit-twc/zimg"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zimg-3.0.6-3-any.pkg.tar.zst"
  sha256 "add647f0b898696b7369098a0779e6df4499de20080b6888ada565ac987cef26"
  license "WTFPL"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/sekrit-twc/zimg/archive/refs/tags/release-3.0.6.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"zimg.h"
    assert_path_exists lib/"libzimg.dll.a"
    assert_path_exists bin/"libzimg-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/zimg.pc").read
  end
end
