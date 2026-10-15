class Odpi < Formula
  desc "Oracle Database Programming Interface for Drivers and Applications"
  homepage "https://oracle.github.io/odpi/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-odpi-5.6.4-1-any.pkg.tar.zst"
  sha256 "2164727918772362d6d13c2bf7291a56cca641ffc77c7a16a315afbb79c58a1d"
  license any_of: ["Apache-2.0", "UPL-1.0"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/oracle/odpi/archive/refs/tags/v26.0.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"dpi.h"
    assert_path_exists lib/"libodpic.dll.a"
    assert_path_exists bin/"libodpic.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/odpi.pc").read
  end
end
