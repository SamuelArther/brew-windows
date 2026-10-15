class Opencsg < Formula
  desc "Constructive solid geometry rendering library"
  homepage "https://www.opencsg.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-opencsg-1.8.2-1-any.pkg.tar.zst"
  sha256 "95da8a4e9db7d5176fd5ed265e11109621e700c7e2906cea452d509b6f2d7eb8"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :homepage
    regex(/href=.*?OpenCSG[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"opencsg.h"
    assert_path_exists lib/"libopencsg.dll.a"
    assert_path_exists bin/"libopencsg-1.dll"
  end
end
