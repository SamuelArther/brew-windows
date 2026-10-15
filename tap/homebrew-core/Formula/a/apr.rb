class Apr < Formula
  desc "Apache Portable Runtime library"
  homepage "https://apr.apache.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-apr-1.7.6-1-any.pkg.tar.zst"
  sha256 "b35200845b62cb6d62d671d0e9e7b4e5cd2ee018648ba59c0097f7c31d886abb"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://www.apache.org/dyn/closer.lua?path=apr/apr-1.7.6.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"apr-1/apr.h"
    assert_path_exists lib/"libapr-1.dll.a"
    assert_path_exists bin/"libapr-1-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/apr-1.pc").read
  end
end
