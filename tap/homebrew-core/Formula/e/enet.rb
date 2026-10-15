class Enet < Formula
  desc "Provides a network communication layer on top of UDP"
  homepage "http://enet.bespin.org"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-enet-1.3.18-1-any.pkg.tar.zst"
  sha256 "6592b96492e43cd4b6368a1fef509b0d7e5b7e745442227f21f97d93225dce06"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "http://enet.bespin.org/Downloads.html"
    regex(/href=.*?enet[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"enet/enet.h"
    assert_path_exists lib/"libenet.dll.a"
    assert_path_exists bin/"libenet-7.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libenet.pc").read
  end
end
