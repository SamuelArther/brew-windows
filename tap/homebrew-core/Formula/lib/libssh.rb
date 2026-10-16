class Libssh < Formula
  desc "C library SSHv1/SSHv2 client and server protocols"
  homepage "https://www.libssh.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libssh-0.12.2-1-any.pkg.tar.zst"
  sha256 "1be494f5fad8bf1210a1eeaccdd07d3e8319f4d13cd79a9302ae196fcc96da8b"
  license "LGPL-2.1-or-later"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://www.libssh.org/files/0.12/libssh-0.12.2.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libssh/sftp.h"
    assert_path_exists lib/"libssh.dll.a"
    assert_path_exists bin/"libssh.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libssh.pc").read
  end
end
