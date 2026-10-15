class Schroedinger < Formula
  desc "High-speed implementation of the Dirac codec"
  homepage "https://launchpad.net/schroedinger"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-schroedinger-1.0.11-6-any.pkg.tar.zst"
  sha256 "8f788452f1549721be2bd1762d97b21e85b40c401f88b5173a6126efae7bbbe5"
  license any_of: ["MPL-1.1", "LGPL-2.0-only", "GPL-2.0-only", "MIT"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://launchpad.net/schroedinger/trunk/1.0.11/+download/schroedinger-1.0.11.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"schroedinger-1.0/schroedinger/schro.h"
    assert_path_exists lib/"libschroedinger-1.0.dll.a"
    assert_path_exists bin/"libschroedinger-1.0-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/schroedinger-1.0.pc").read
  end
end
