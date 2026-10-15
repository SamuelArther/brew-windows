class Libtermkey < Formula
  desc "Library for processing keyboard entry from the terminal"
  homepage "https://www.leonerd.org.uk/code/libtermkey/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtermkey-0.22-2-any.pkg.tar.zst"
  sha256 "e30a4c9296a4032e95a472312c480a5ed03495abe1e33411d3919973473fd7f7"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :homepage
    regex(/href=.*?libtermkey[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"termkey.h"
    assert_path_exists lib/"libtermkey.dll.a"
    assert_path_exists bin/"libtermkey-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/termkey.pc").read
  end
end
