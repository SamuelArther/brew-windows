class Wslay < Formula
  desc "C websocket library"
  homepage "https://wslay.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-wslay-1.1.1-3-any.pkg.tar.zst"
  sha256 "274bf283ff670c45c7d36d69635931481dfa87878eb802f3c6a25b1557f6f972"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/tatsuhiro-t/wslay/releases/download/release-1.1.1/wslay-1.1.1.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"wslay/wslay.h"
    assert_path_exists lib/"libwslay.dll.a"
    assert_path_exists bin/"libwslay-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libwslay.pc").read
  end
end
