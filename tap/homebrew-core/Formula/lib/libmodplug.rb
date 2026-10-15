class Libmodplug < Formula
  desc "Library from the Modplug-XMMS project"
  homepage "https://modplug-xmms.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libmodplug-0.8.9.0-5-any.pkg.tar.zst"
  sha256 "7bac0ddfde760dbcee6e07ea0e8b32cd560f53deec11edfa5391337ccc6158b5"
  license :public_domain

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/modplug-xmms/libmodplug/0.8.9.0/libmodplug-0.8.9.0.tar.gz"
    regex(%r{url=.*?/libmodplug[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libmodplug/stdafx.h"
    assert_path_exists lib/"libmodplug.dll.a"
    assert_path_exists bin/"libmodplug-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libmodplug.pc").read
  end
end
