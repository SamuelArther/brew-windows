class Sratom < Formula
  desc "Library for serializing LV2 atoms to/from RDF"
  homepage "https://drobilla.net/software/sratom.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sratom-0.6.22-1-any.pkg.tar.zst"
  sha256 "ce3260dce4a345c51f174a24755e55879122e420c60c1ede23a1bc0e9fe04720"
  license "ISC"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.drobilla.net"
    regex(/href=.*?sratom[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"sratom-0/sratom/sratom.h"
    assert_path_exists lib/"libsratom-0.dll.a"
    assert_path_exists bin/"libsratom-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/sratom-0.pc").read
  end
end
