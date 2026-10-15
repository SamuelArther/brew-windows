class Libnpupnp < Formula
  desc "C++ base UPnP library, derived from Portable UPnP, a.k.a libupnp"
  homepage "https://www.lesbonscomptes.com/upmpdcli/npupnp-doc/libnpupnp.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libnpupnp-6.2.1-2-any.pkg.tar.zst"
  sha256 "20f67641843df28384014071ad519cf6b2ff168ce14c426db81b0a9331aa94de"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://www.lesbonscomptes.com/upmpdcli/downloads/"
    regex(/href=.*?libnpupnp[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"npupnp/upnp.h"
    assert_path_exists lib/"libnpupnp.dll.a"
    assert_path_exists bin/"libnpupnp.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libnpupnp.pc").read
  end
end
