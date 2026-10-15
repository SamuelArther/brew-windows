class Opus < Formula
  desc "Audio codec"
  homepage "https://www.opus-codec.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-opus-1.6.1-1-any.pkg.tar.zst"
  sha256 "7b8f176f9cc0431b52006861c0a2e083d73d4b47a73ab749755b5379ff673252"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ftp.osuosl.org/pub/xiph/releases/opus/"
    regex(%r{href=(?:["']?|.*?/)opus[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"opus/opus.h"
    assert_path_exists lib/"libopus.dll.a"
    assert_path_exists bin/"libopus-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/opus.pc").read
  end
end
