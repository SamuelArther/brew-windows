class Qxmpp < Formula
  desc "Cross-platform C++ XMPP client and server library"
  homepage "https://invent.kde.org/libraries/qxmpp"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-qxmpp-1.17.0-1-any.pkg.tar.zst"
  sha256 "cc1fe4136e7a5542a774fe6b2b9d0d3250640714f5a3813e28593b1d4dc34f5d"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://invent.kde.org/libraries/qxmpp/-/archive/v1.17.0/qxmpp-v1.17.0.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"QXmppQt6/QXmppIq.h"
    assert_path_exists lib/"libQXmppQt6.dll.a"
    assert_path_exists bin/"libQXmppQt6.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/QXmppQt6.pc").read
  end
end
