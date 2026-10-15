class Libxc < Formula
  desc "Library of exchange and correlation functionals for codes"
  homepage "https://libxc.gitlab.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libxc-7.1.2-2-any.pkg.tar.zst"
  sha256 "3ecec43cbad25d2cf13aeca8daaf45980ad9818357669f5ac5b906b648149cf3"
  license "MPL-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://gitlab.com/libxc/libxc/-/archive/7.1.2/libxc-7.1.2.tar.bz2"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/xc-info.exe", "xc-info"
  end

  test do
    assert_path_exists bin/"xc-info.exe"
  end
end
