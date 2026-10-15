class CmakeDocs < Formula
  desc "Documentation for CMake"
  homepage "https://www.cmake.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cmake-docs-4.4.4-3-any.pkg.tar.zst"
  sha256 "2910e5a466bd2bc144dcd422691bc22fc77892ffa8d026d67488d6836635b283"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/Kitware/CMake/releases/download/v4.4.4/cmake-4.4.4.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    assert_path_exists share/"doc/cmake/CMake.qch"
  end
end
