class Cpputest < Formula
  desc "C /C++ based unit xUnit test framework"
  homepage "https://cpputest.github.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cpputest-4.0-3-any.pkg.tar.zst"
  sha256 "e8d509284735e26eb4805cb1c1fd6f098776b6e83ef16b1f6ffc67f0b2ef67a9"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/cpputest/cpputest/releases/download/v4.0/cpputest-4.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"CppUTest/Utest.h"
    assert_match "Version: #{version}", (lib/"pkgconfig/cpputest.pc").read
  end
end
