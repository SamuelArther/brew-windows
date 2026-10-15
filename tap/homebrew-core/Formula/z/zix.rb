class Zix < Formula
  desc "C99 portability and data structure library"
  homepage "https://gitlab.com/drobilla/zix"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zix-0.8.0-1-any.pkg.tar.zst"
  sha256 "3639aad2daafd7d607da396ad2d5c81aba9b1e6ca11aedda9d09c833e238091d"
  license "ISC"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.com/drobilla/zix/-/archive/v0.8.2/zix-v0.8.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"zix-0/zix/sem.h"
    assert_path_exists lib/"libzix-0.dll.a"
    assert_path_exists bin/"libzix-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/zix-0.pc").read
  end
end
