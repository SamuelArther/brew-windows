class Xorgproto < Formula
  desc "X.Org: Protocol Headers"
  homepage "https://www.x.org/"
  url "https://repo.msys2.org/msys/x86_64/xorgproto-2026.1-1-any.pkg.tar.zst"
  sha256 "7c256bde7d84700ff4ef45b44f39e7719a0cd1ebf501ab531dbcbb8f32a8f73e"
  license "MIT"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://xorg.freedesktop.org/archive/individual/proto/xorgproto-2026.1.tar.gz"
    regex(/href=.*?xorgproto[._-]v?(\d+\.\d+(?:\.([0-8]\d*?)?\d(?:\.\d+)*)?)\.t/i)
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"X11/X.h"
  end
end
