class Libslirp < Formula
  desc "General purpose TCP-IP emulator"
  homepage "https://gitlab.freedesktop.org/slirp/libslirp"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libslirp-4.9.3-1-any.pkg.tar.zst"
  sha256 "c982be43767a950f220b498f0f212eb2e2c945d93c968bd0e4f66b1b5da94f6c"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.freedesktop.org/slirp/libslirp/-/archive/v4.9.5/libslirp-v4.9.5.tar.gz"
    regex(/^v(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"slirp/libslirp.h"
    assert_path_exists lib/"libslirp.dll.a"
    assert_path_exists bin/"libslirp-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/slirp.pc").read
  end
end
