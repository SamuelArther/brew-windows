class Unibilium < Formula
  desc "Very basic terminfo library"
  homepage "https://github.com/neovim/unibilium"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-unibilium-2.1.2-1-any.pkg.tar.zst"
  sha256 "7c50c17db7697a2ae7b3db232aff2e498fe0cbb7c7810b706082ad2d2182061e"
  license "LGPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/neovim/unibilium/archive/refs/tags/v2.1.4.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"unibilium.h"
    assert_path_exists lib/"libunibilium.dll.a"
    assert_path_exists bin/"libunibilium-4.dll"
  end
end
