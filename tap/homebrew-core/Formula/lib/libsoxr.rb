class Libsoxr < Formula
  desc "High quality, one-dimensional sample-rate conversion library"
  homepage "https://sourceforge.net/projects/soxr/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libsoxr-0.1.3-6-any.pkg.tar.zst"
  sha256 "29ace511f4ca8c2f58a218837478eb15b99246690849ef8e6d679cc7fc70fe69"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/soxr/soxr-0.1.3-Source.tar.xz"
    regex(%r{url=.*?/soxr[._-]v?(\d+(?:\.\d+)+)(?:-Source)?\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"soxr.h"
    assert_path_exists lib/"libsoxr-lsr.dll.a"
    assert_path_exists bin/"libsoxr-lsr.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/soxr-lsr.pc").read
  end
end
