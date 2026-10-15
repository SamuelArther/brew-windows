class Graphene < Formula
  desc "Thin layer of graphic data types"
  homepage "https://ebassi.github.io/graphene/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-graphene-1.10.8-3-any.pkg.tar.zst"
  sha256 "4ae49208acf49de84f2abbe9555c2d22f51ab52c1509f5ac8bb5ce7fcaf07d57"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/ebassi/graphene/archive/refs/tags/1.10.8.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"graphene-1.0/graphene.h"
    assert_path_exists lib/"libgraphene-1.0.dll.a"
    assert_path_exists bin/"libgraphene-1.0-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/graphene-1.0.pc").read
  end
end
