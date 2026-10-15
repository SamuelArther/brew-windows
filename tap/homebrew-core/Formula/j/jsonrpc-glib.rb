class JsonrpcGlib < Formula
  desc "GNOME library to communicate with JSON-RPC based peers"
  homepage "https://gitlab.gnome.org/GNOME/jsonrpc-glib"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jsonrpc-glib-3.44.2-2-any.pkg.tar.zst"
  sha256 "e9dee86e666d2bb53e84c3b9e7ec01cf8c03d6271fb6de6a89905d192c1651e6"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/jsonrpc-glib/3.44/jsonrpc-glib-3.44.2.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"jsonrpc-glib-1.0/jsonrpc-glib.h"
    assert_path_exists lib/"libjsonrpc-glib-1.0.dll.a"
    assert_path_exists bin/"libjsonrpc-glib-1.0-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/jsonrpc-glib-1.0.pc").read
  end
end
