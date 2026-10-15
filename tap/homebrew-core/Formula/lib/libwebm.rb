class Libwebm < Formula
  desc "WebM container"
  homepage "https://www.webmproject.org/code/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwebm-1.0.0.32-1-any.pkg.tar.zst"
  sha256 "99c2363530aea78b9bd551ec7137d2e164579315d5a7e7d364366eec8060d5f9"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/webmproject/libwebm/archive/refs/tags/libwebm-1.0.0.32.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"webm/id.h"
    assert_path_exists lib/"libwebm.dll.a"
    assert_path_exists bin/"libwebm.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libwebm.pc").read
  end
end
