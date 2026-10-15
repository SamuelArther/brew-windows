class Libunibreak < Formula
  desc "Implementation of the Unicode line- and word-breaking algorithms"
  homepage "https://github.com/adah1972/libunibreak"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libunibreak-7.0-1-any.pkg.tar.zst"
  sha256 "de657828b41248a2fd67fa6e8348402a61ef130b0444f1f006c6545c43ad86d8"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/adah1972/libunibreak/releases/download/libunibreak_8_0/libunibreak-8.0.tar.gz"
    regex(/v?(\d+(?:[_-]\d+)+)$/i)
    strategy :git do |tags|
      tags.map { |tag| tag[regex, 1]&.tr("_", ".") }
    end
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"linebreak.h"
    assert_path_exists lib/"libunibreak.dll.a"
    assert_path_exists bin/"libunibreak-7.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libunibreak.pc").read
  end
end
