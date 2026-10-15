class Log4cxx < Formula
  desc "Library of C++ classes for flexible logging"
  homepage "https://logging.apache.org/log4cxx/index.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-log4cxx-1.8.0-1-any.pkg.tar.zst"
  sha256 "ea48660a37c3850354170aaf209fcf2ad44666deae1b1eb8314fe0b0f835042b"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://www.apache.org/dyn/closer.lua?path=logging/log4cxx/1.8.0/apache-log4cxx-1.8.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"log4cxx/mdc.h"
    assert_path_exists lib/"liblog4cxx-qt.dll.a"
    assert_path_exists bin/"liblog4cxx-15.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/liblog4cxx-qt.pc").read
  end
end
