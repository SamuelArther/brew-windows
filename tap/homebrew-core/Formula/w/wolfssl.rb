class Wolfssl < Formula
  desc "Embedded SSL Library written in C"
  homepage "https://www.wolfssl.com"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-wolfssl-5.9.4-1-any.pkg.tar.zst"
  sha256 "0a655158c6aca7afa2f179df0020c365407569c6d82ff46608dc551b40e7243d"
  license "GPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/wolfSSL/wolfssl.git"
    regex(/v?(\d+(?:\.\d+)+)[._-]stable/i)
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"wolfssl/crl.h"
    assert_path_exists lib/"libwolfssl.dll.a"
    assert_path_exists bin/"libwolfssl-46.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/wolfssl.pc").read
  end
end
