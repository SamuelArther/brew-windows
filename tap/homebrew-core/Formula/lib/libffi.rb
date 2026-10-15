class Libffi < Formula
  desc "Portable Foreign Function Interface library"
  homepage "https://sourceware.org/libffi/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libffi-3.8.0-1-any.pkg.tar.zst"
  sha256 "99ad12f4ecfa00a889ef9c5c0188592368a5bac109d0c508ce4e04f29ea95a76"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libffi/libffi/releases/download/v3.8.0/libffi-3.8.0.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ffi.h"
    assert_path_exists lib/"libffi.dll.a"
    assert_path_exists bin/"libffi-8.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libffi.pc").read
  end
end
