class Threadweaver < Formula
  desc "Helper for multithreaded programming"
  homepage "https://api.kde.org/threadweaver-index.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-threadweaver-6.31.0-1-any.pkg.tar.zst"
  sha256 "ea9002a068cdaeaf140312e5921a9565d18804613c293a494a77d494d762a2a7"
  license "LGPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.kde.org/stable/frameworks/"
    regex(%r{href=.*?v?(\d+(?:\.\d+)+)/?["' >]}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"KF6/ThreadWeaver/threadweaver_version.h"
    assert_path_exists lib/"libKF6ThreadWeaver.dll.a"
    assert_path_exists bin/"libKF6ThreadWeaver.dll"
  end
end
