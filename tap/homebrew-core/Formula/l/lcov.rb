class Lcov < Formula
  desc "Graphical front-end for GCC's coverage testing tool (gcov)"
  homepage "https://github.com/linux-test-project/lcov"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lcov-1.16-1-any.pkg.tar.zst"
  sha256 "bb8112f00e9f7fae0880a87bdea30ab2dab252a600749a81c218c748f1b967ab"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/linux-test-project/lcov/releases/download/v2.6/lcov-2.6.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    assert_path_exists bin/"gendesc"
  end
end
