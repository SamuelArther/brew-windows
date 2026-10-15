class Cpr < Formula
  desc "C++ Requests, a spiritual port of Python Requests"
  homepage "https://docs.libcpr.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cpr-1.14.1-2-any.pkg.tar.zst"
  sha256 "19b8dd4205b01636a0999c8b4f2ba54c441ef48903b396367f22800c4a05f3a2"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libcpr/cpr/archive/refs/tags/1.15.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"cpr/api.h"
    assert_path_exists lib/"libcpr.dll.a"
    assert_path_exists bin/"libcpr-1.dll"
  end
end
