class Wiiuse < Formula
  desc "Connect Nintendo Wii Remotes"
  homepage "https://github.com/wiiuse/wiiuse"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-wiiuse-0.15.7-1-any.pkg.tar.zst"
  sha256 "70ffc62557100ae763577bfde24712709d8ede1987fc978f259496a6bd03369d"
  license "GPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/wiiuse/wiiuse/archive/refs/tags/0.16.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"wiiuse.h"
    assert_path_exists lib/"libwiiuse.dll.a"
    assert_path_exists bin/"libwiiuse.dll"
  end
end
