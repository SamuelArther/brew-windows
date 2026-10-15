class Libbsd < Formula
  desc "Utility functions from BSD systems"
  homepage "https://libbsd.freedesktop.org/"
  url "https://repo.msys2.org/msys/x86_64/libbsd-0.12.2-1-x86_64.pkg.tar.zst"
  sha256 "78cc6404b26ae3d698d0d91b54ffc399c018506ad291177e63f2161c46ac755c"
  license all_of: [
    "BSD-3-Clause",
    "BSD-2-Clause",
    "Beerware",
    "ISC",
    "libutil-David-Nugent",
    "MIT",
    :public_domain,
  ]

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://libbsd.freedesktop.org/releases/"
    regex(/href=.*?libbsd[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"bsd/bsd.h"
    assert_path_exists lib/"libbsd.dll.a"
    assert_path_exists bin/"msys-bsd-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libbsd-overlay.pc").read
  end
end
