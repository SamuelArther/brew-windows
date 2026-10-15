class LibomemoC < Formula
  desc "Implementation of Signal's ratcheting forward secrecy protocol"
  homepage "https://github.com/dino/libomemo-c"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libomemo-c-0.5.1-1-any.pkg.tar.zst"
  sha256 "4c5ee137f989883701f4fd0136d764dcaf0f5dc69f38dae4e39f16eef378ea24"
  license "GPL-3.0-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/dino/libomemo-c/archive/refs/tags/v0.5.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"omemo/hkdf.h"
    assert_path_exists lib/"libomemo-c.dll.a"
    assert_path_exists bin/"libomemo-c-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libomemo-c.pc").read
  end
end
