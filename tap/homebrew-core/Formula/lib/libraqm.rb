class Libraqm < Formula
  desc "Library for complex text layout"
  homepage "https://github.com/HOST-Oman/libraqm"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libraqm-0.11.0-1-any.pkg.tar.zst"
  sha256 "a4b66d786fb57d291a083209197f8e324f5e88b3332e889926e64ba521c7e279"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/HOST-Oman/libraqm/archive/refs/tags/v0.11.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"raqm.h"
    assert_path_exists lib/"libraqm.dll.a"
    assert_path_exists bin/"libraqm-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/raqm.pc").read
  end
end
