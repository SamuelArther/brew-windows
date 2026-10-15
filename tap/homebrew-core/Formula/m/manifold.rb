class Manifold < Formula
  desc "Geometry library for topological robustness"
  homepage "https://github.com/elalish/manifold"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-manifold-3.4.1-1-any.pkg.tar.zst"
  sha256 "eb7c33c5b6f956f4ed961604337633ad7638480444c21b4102875974ff74db11"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/elalish/manifold/releases/download/v3.5.4/manifold-3.5.4.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"manifold/types.h"
    assert_path_exists lib/"libmanifold.dll.a"
    assert_path_exists bin/"libmanifold.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/manifold.pc").read
  end
end
