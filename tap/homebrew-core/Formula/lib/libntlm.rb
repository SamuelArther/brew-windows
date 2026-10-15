class Libntlm < Formula
  desc "Implements Microsoft's NTLM authentication"
  homepage "https://gitlab.com/gsasl/libntlm/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libntlm-1.8-2-any.pkg.tar.zst"
  sha256 "3895f0abefb8db42bb1b318665f2f9579012ca86afce5829f12d2d751ac63361"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.savannah.nongnu.org/releases/libntlm/"
    regex(/href=.*?libntlm[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ntlm.h"
    assert_path_exists lib/"libntlm.dll.a"
    assert_path_exists bin/"libntlm-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libntlm.pc").read
  end
end
