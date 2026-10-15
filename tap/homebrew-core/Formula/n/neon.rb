class Neon < Formula
  desc "HTTP and WebDAV client library with a C interface"
  homepage "https://notroj.github.io/neon/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-neon-0.36.0-2-any.pkg.tar.zst"
  sha256 "e9ae5623f1c5964379a191bd8074e23edb5a8191b9c209c907017cc0f475c13c"
  license "LGPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :homepage
    regex(/href=.*?neon[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"neon/ne_207.h"
    assert_path_exists lib/"libneon.dll.a"
    assert_path_exists bin/"libneon-27.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/neon.pc").read
  end
end
