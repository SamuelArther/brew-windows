class AprUtil < Formula
  desc "Companion library to apr, the Apache Portable Runtime library"
  homepage "https://apr.apache.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-apr-util-1.6.5-1-any.pkg.tar.zst"
  sha256 "c9d5d02cc8511d1b130e5f9356c738814389bfe7670ee6b81102c47bc5d4f390"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://www.apache.org/dyn/closer.lua?path=apr/apr-util-1.6.5.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"apr-1/apu.h"
    assert_path_exists lib/"libaprutil-1.dll.a"
    assert_path_exists bin/"libaprutil-1-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/apr-util-1.pc").read
  end
end
