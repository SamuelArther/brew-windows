class Ntl < Formula
  desc "C++ number theory library"
  homepage "https://libntl.org"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ntl-11.6.0-4-any.pkg.tar.zst"
  sha256 "00a0fd2e1265a0344a2ca774c8cd29218fd3e2cfcd6f51c832f385783eb62333"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://libntl.org/download.html"
    regex(/href=.*?ntl[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"NTL/PD.h"
    assert_path_exists lib/"libntl.dll.a"
    assert_path_exists bin/"libntl-45.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/ntl.pc").read
  end
end
