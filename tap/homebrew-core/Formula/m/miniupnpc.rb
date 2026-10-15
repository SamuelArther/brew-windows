class Miniupnpc < Formula
  desc "UPnP IGD client library and daemon"
  homepage "http://miniupnp.free.fr/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-miniupnpc-2.3.3-4-any.pkg.tar.zst"
  sha256 "0729009682c1c012dd53c9b83d2d3f6b520c4cab33bfcdc09f83bf7c616a3e54"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/miniupnp/miniupnp/releases/download/miniupnpc_2_3_3/miniupnpc-2.3.3.tar.gz"
    regex(/^miniupnpc[._-]v?(\d+(?:[._]\d+)+)$/i)
    strategy :git do |tags, regex|
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
    assert_path_exists include/"miniupnpc/upnpdev.h"
    assert_path_exists lib/"libminiupnpc.dll.a"
    assert_path_exists bin/"libminiupnpc.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/miniupnpc.pc").read
  end
end
