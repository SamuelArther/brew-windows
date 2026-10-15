class Popt < Formula
  desc "Library like getopt(3) with a number of enhancements"
  homepage "https://github.com/rpm-software-management/popt"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-popt-1.19-2-any.pkg.tar.zst"
  sha256 "4cef77364f9f131d6cb325ad65d089e956f06b48eafbcc3a300426231195afa8"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :homepage
    regex(/^(?:popt[._-])?v?(\d+(?:[._]\d+)+)(?:[._-]release)?$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"popt.h"
    assert_path_exists lib/"libpopt.dll.a"
    assert_path_exists bin/"libpopt-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/popt.pc").read
  end
end
