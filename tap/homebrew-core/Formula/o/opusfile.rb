class Opusfile < Formula
  desc "API for decoding and seeking in .opus files"
  homepage "https://www.opus-codec.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-opusfile-0.12-4-any.pkg.tar.zst"
  sha256 "8a9fa96bcba9ae6caf77b9834e7c3a4072078761aeb7aa8121967e3f65faf656"
  license "BSD-3-Clause"
  revision 3

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ftp.osuosl.org/pub/xiph/releases/opus/"
    regex(%r{href=(?:["']?|.*?/)opusfile[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"opus/opusfile.h"
    assert_path_exists lib/"libopusfile.dll.a"
    assert_path_exists bin/"libopusfile-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/opusfile.pc").read
  end
end
