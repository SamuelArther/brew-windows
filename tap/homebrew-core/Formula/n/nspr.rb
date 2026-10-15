class Nspr < Formula
  desc "Platform-neutral API for system-level and libc-like functions"
  homepage "https://hg.mozilla.org/projects/nspr"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-nspr-4.40-1-any.pkg.tar.zst"
  sha256 "35f0f60025cb0d20039f3d2c59df744a9308fe264c4606d49685e87efce5984a"
  license "MPL-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ftp.mozilla.org/pub/nspr/releases/"
    regex(%r{href=.*?v?(\d+(?:\.\d+)+)/?["' >]}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"nspr/nspr.h"
    assert_path_exists lib/"libnspr4.dll.a"
    assert_path_exists bin/"libnspr4.dll"
  end
end
