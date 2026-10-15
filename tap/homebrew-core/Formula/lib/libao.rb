class Libao < Formula
  desc "Cross-platform Audio Library"
  homepage "https://www.xiph.org/ao/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libao-1.2.2-3-any.pkg.tar.zst"
  sha256 "91a0cf18a7e71ec11362c6b71774d7d875ff8ba2b52686a2a3a64a818964940e"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.xiph.org/xiph/libao/-/archive/1.2.2/libao-1.2.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ao/ao.h"
    assert_path_exists lib/"libao.dll.a"
    assert_path_exists bin/"libao-4.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/ao.pc").read
  end
end
