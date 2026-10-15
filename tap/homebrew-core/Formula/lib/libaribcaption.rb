class Libaribcaption < Formula
  desc "Portable ARIB STD-B24 Caption Decoder/Renderer"
  homepage "https://github.com/xqq/libaribcaption"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libaribcaption-1.1.2-1-any.pkg.tar.zst"
  sha256 "fc2157116771c4179466a766d585998635122ed4e4546daeffb29cf688e399c0"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/xqq/libaribcaption/archive/refs/tags/v1.1.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"aribcaption/color.h"
    assert_path_exists lib/"libaribcaption.dll.a"
    assert_path_exists bin/"libaribcaption.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libaribcaption.pc").read
  end
end
