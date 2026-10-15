class Libksba < Formula
  desc "X.509 and CMS library"
  homepage "https://www.gnupg.org/related_software/libksba/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libksba-1.6.8-1-any.pkg.tar.zst"
  sha256 "2fdd5666c9495981614009af931317c18751b8afe7b0c1344cec33e4b875f98f"
  license any_of: ["LGPL-3.0-or-later", "GPL-2.0-or-later"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gnupg.org/ftp/gcrypt/libksba/"
    regex(/href=.*?libksba[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ksba.h"
    assert_path_exists lib/"libksba.dll.a"
    assert_path_exists bin/"libksba-8.dll"
  end
end
