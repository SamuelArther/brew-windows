class Opensubdiv < Formula
  desc "Open-source subdivision surface library"
  homepage "https://www.opensubdiv.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-opensubdiv-3.7.0-1-any.pkg.tar.zst"
  sha256 "f5ae59671a470ebc7021cfff87241945ce94bbe969abcd7fd30b62cb42c5ff40"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/PixarAnimationStudios/OpenSubdiv/archive/refs/tags/v3_7_0.tar.gz"
    regex(/^v?(\d+(?:[._]\d+)+)$/i)
    strategy :git do |tags, regex|
      tags.filter_map { |tag| tag[regex, 1]&.tr("_", ".") }
    end
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"opensubdiv/version.h"
    assert_path_exists lib/"libosdCPU.dll.a"
    assert_path_exists bin/"libosdCPU.dll"
  end
end
