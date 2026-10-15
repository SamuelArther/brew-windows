class Libuv < Formula
  desc "Multi-platform support library with a focus on asynchronous I/O"
  homepage "https://libuv.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libuv-1.53.0-1-any.pkg.tar.zst"
  sha256 "5a89b3afb8ce3a2a6e64704f7fe2ffbf57e1fc88d6033b8dae09a4c74328bcbb"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :head
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"uv.h"
    assert_path_exists lib/"libuv.dll.a"
    assert_path_exists bin/"libuv-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libuv-static.pc").read
  end
end
