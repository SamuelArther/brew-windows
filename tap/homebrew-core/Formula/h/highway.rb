class Highway < Formula
  desc "Performance-portable, length-agnostic SIMD with runtime dispatch"
  homepage "https://github.com/google/highway"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-highway-1.4.0-3-any.pkg.tar.zst"
  sha256 "4d2ed7c528c2323e1bf05921963181412b7a33eee1cde798fdd7e613f59b2ab3"
  license any_of: ["Apache-2.0", "BSD-3-Clause"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/google/highway/archive/refs/tags/1.4.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"hwy/base.h"
    assert_path_exists lib/"libhwy.dll.a"
    assert_path_exists bin/"libhwy.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libhwy-contrib.pc").read
  end
end
