class Libcue < Formula
  desc "Cue sheet parser library for C"
  homepage "https://github.com/lipnitsk/libcue"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libcue-2.3.0-1-any.pkg.tar.zst"
  sha256 "555d869359bf0d01716495bf8c85e4b8b9eabeef0d6e07b944ac0e405a0e45cc"
  license "GPL-2.0-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/lipnitsk/libcue/archive/refs/tags/v2.3.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libcue.h"
    assert_path_exists lib/"libcue.dll.a"
    assert_path_exists bin/"libcue.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libcue.pc").read
  end
end
