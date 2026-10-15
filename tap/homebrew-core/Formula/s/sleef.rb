class Sleef < Formula
  desc "SIMD library for evaluating elementary functions"
  homepage "https://sleef.org"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sleef-3.9.0-3-any.pkg.tar.zst"
  sha256 "a613f2e6f0cc402e0e34014374121cc05c7e078cffaf44444b85c45742fe7f5b"
  license "BSL-1.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/shibatch/sleef/archive/refs/tags/3.9.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"sleef.h"
    assert_path_exists lib/"libsleef.dll.a"
    assert_path_exists bin/"libsleef-3.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/sleef.pc").read
  end
end
