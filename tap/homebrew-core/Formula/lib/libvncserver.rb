class Libvncserver < Formula
  desc "VNC server and client libraries"
  homepage "https://libvnc.github.io"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libvncserver-0.9.15-3-any.pkg.tar.zst"
  sha256 "4f701f72b24d33e69e48d2bec1d59813f7e8a046784deb9984824dd06544a9be"
  license "GPL-2.0-or-later"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/LibVNC/libvncserver/archive/refs/tags/LibVNCServer-0.9.15.tar.gz"
    regex(/^LibVNCServer[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"rfb/rfb.h"
    assert_path_exists lib/"libvncclient.dll.a"
    assert_path_exists bin/"libvncclient.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libvncclient.pc").read
  end
end
