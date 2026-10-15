class Libevent < Formula
  desc "Asynchronous event library"
  homepage "https://libevent.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libevent-2.1.13-1-any.pkg.tar.zst"
  sha256 "7b2de9a16231f33a4d949a698a1aa8ca5d2bd5eb3eee64f497024bcbcee7b064"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :homepage
    regex(/libevent[._-]v?(\d+(?:\.\d+)+)-stable/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"evdns.h"
    assert_path_exists lib/"libevent.dll.a"
    assert_path_exists bin/"libevent-7.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libevent.pc").read
  end
end
