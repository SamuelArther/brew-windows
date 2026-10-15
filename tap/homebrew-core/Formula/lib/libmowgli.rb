class Libmowgli < Formula
  desc "Core framework for Atheme applications"
  homepage "https://github.com/atheme/libmowgli-2"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libmowgli-2.1.3-7-any.pkg.tar.zst"
  sha256 "17836a9ae2a44766dbdafc167c321486fc9b4a9c5e094b0b047391bf7946e5b4"
  license "ISC"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/atheme/libmowgli-2/archive/refs/tags/v2.1.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libmowgli-2/mowgli.h"
    assert_path_exists lib/"libmowgli-2.dll.a"
    assert_path_exists bin/"libmowgli-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libmowgli-2.pc").read
  end
end
