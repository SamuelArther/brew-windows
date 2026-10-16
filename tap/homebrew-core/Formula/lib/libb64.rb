class Libb64 < Formula
  desc "Base64 encoding/decoding library"
  homepage "https://libb64.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libb64-1.2.1-1-any.pkg.tar.zst"
  sha256 "2fa0526f59e934ab544b71a87d5ca8088bf8a5ca3036b8142edef7ad213f9cf6"
  license "CC-PDDC"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/libb64/libb64/libb64/libb64-1.2.1.zip"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"b64/decode.h"
    assert_path_exists lib/"libb64.dll.a"
    assert_path_exists bin/"libb64.dll"
  end
end
