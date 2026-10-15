class Log4cpp < Formula
  desc "Configurable logging for C++"
  homepage "https://log4cpp.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-log4cpp-1.1.4-4-any.pkg.tar.zst"
  sha256 "9a63583e200959707a2cca3a55f31a273c8ff56b01dc94f8199eb2f541e2e5f2"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/log4cpp/log4cpp-1.1.x%20%28new%29/log4cpp-1.1/log4cpp-1.1.6.tar.gz"
    regex(%r{url=.*?/log4cpp[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"log4cpp/NDC.hh"
    assert_path_exists lib/"liblog4cpp.dll.a"
    assert_path_exists bin/"liblog4cpp-5.dll"
  end
end
