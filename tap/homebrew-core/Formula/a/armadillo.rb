class Armadillo < Formula
  desc "C++ linear algebra library"
  homepage "https://arma.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-armadillo-15.6.0-1-any.pkg.tar.zst"
  sha256 "e52b67e9a3604ba4f2436f761567a99a6a7c390c27d4488f6a8e3257eaf948ac"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/arma/armadillo-15.6.1.tar.xz"
    regex(%r{url=.*?/armadillo[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"armadillo_bits/fill.hpp"
    assert_path_exists lib/"libarmadillo.dll.a"
    assert_path_exists bin/"libarmadillo.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/armadillo.pc").read
  end
end
