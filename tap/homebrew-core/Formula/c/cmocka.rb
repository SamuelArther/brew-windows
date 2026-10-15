class Cmocka < Formula
  desc "Unit testing framework for C"
  homepage "https://cmocka.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cmocka-1.1.8-3-any.pkg.tar.zst"
  sha256 "5c5b558ed7ad786fd5782292d2d930d55eb832fd1e82602fcaf1a0c54b4a124f"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://cmocka.org/files/2.0/cmocka-2.0.2.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"cmocka.h"
    assert_path_exists lib/"libcmocka.dll.a"
    assert_path_exists bin/"libcmocka.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/cmocka.pc").read
  end
end
