class Libvterm < Formula
  desc "C99 library which implements a VT220 or xterm terminal emulator"
  homepage "https://www.leonerd.org.uk/code/libvterm/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libvterm-0.3.3-1-any.pkg.tar.zst"
  sha256 "c93cf6c76a08326b85952255c6c7331f3accc556ee905a3d78501a73d1d00621"
  license "MIT"
  version_scheme 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://launchpad.net/libvterm/trunk/v0.3/+download/libvterm-0.3.3.tar.gz"
    regex(/href=.*?libvterm[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"vterm.h"
    assert_path_exists lib/"libvterm.dll.a"
    assert_path_exists bin/"libvterm-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/vterm.pc").read
  end
end
