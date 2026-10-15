class Flint < Formula
  desc "C library for number theory"
  homepage "https://flintlib.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-flint-3.6.0-3-any.pkg.tar.zst"
  sha256 "f0d9ee145432bf393db5a3b20d93df0bfd97be123d50e1db653c9183ac0e59af"
  license "LGPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/flintlib/flint/releases/download/v3.6.0/flint-3.6.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+(?:[._-]?p\d+)?)$/i)
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"flint/ca.h"
    assert_path_exists lib/"libflint.dll.a"
    assert_path_exists bin/"libflint-24.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/flint.pc").read
  end
end
