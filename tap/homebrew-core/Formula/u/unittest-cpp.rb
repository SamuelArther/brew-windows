class UnittestCpp < Formula
  desc "Unit testing framework for C++"
  homepage "https://github.com/unittest-cpp/unittest-cpp"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-unittest-cpp-2.0.0-3-any.pkg.tar.zst"
  sha256 "27d6431fcf2b2101f80cd889c60e160021b00abb01a50d4539f7abc84600ff98"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/unittest-cpp/unittest-cpp/releases/download/v2.0.0/unittest-cpp-2.0.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"UnitTest++/Test.h"
    assert_match "Version: #{version}", (lib/"pkgconfig/UnitTest++.pc").read
  end
end
