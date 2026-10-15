class DirectxHeaders < Formula
  desc "Official DirectX headers available under an open source license"
  homepage "https://devblogs.microsoft.com/directx/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-directx-headers-1~1.619.5-1-any.pkg.tar.zst"
  version "1.619.5"
  sha256 "e3524c39e8b0ead83f336e67b7b1b520fdcb801c6eb9c6134cbdbd196d67deb8"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/microsoft/DirectX-Headers/archive/refs/tags/v1.619.5.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"directx/d3d12.h"
    assert_match "Version: #{version}", (lib/"pkgconfig/DirectX-Headers.pc").read
  end
end
