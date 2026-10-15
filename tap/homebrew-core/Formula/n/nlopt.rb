class Nlopt < Formula
  desc "Free/open-source library for nonlinear optimization"
  homepage "https://nlopt.readthedocs.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-nlopt-2.11.0-1-any.pkg.tar.zst"
  sha256 "c44a8bb342aaa178f066c6882befa638887990a73efb55d8e4d062e55978aeee"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/stevengj/nlopt/archive/refs/tags/v2.11.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"nlopt.h"
    assert_path_exists lib/"libnlopt.dll.a"
    assert_path_exists bin/"libnlopt.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/nlopt.pc").read
  end
end
