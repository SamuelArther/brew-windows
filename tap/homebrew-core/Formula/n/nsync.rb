class Nsync < Formula
  desc "C library that exports various synchronization primitives"
  homepage "https://github.com/google/nsync"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-nsync-1.30.0-3-any.pkg.tar.zst"
  sha256 "4847a85bf71d70d1a215f228ed307d8c6a7657b59e46ce25814672c14bcc9bbd"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/google/nsync/archive/refs/tags/1.30.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"nsync.h"
    assert_path_exists lib/"libnsync.dll.a"
    assert_path_exists bin/"libnsync.dll"
  end
end
