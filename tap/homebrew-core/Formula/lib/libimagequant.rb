class Libimagequant < Formula
  desc "Palette quantization library extracted from pnquant2"
  homepage "https://pngquant.org/lib/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libimagequant-4.4.1-1-any.pkg.tar.zst"
  sha256 "8966b692f5cc1b7c2f97ca045075334d4972eefcb03832b6723e1bbe8e5f79e7"
  license all_of: ["GPL-3.0-or-later", "HPND"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/ImageOptim/libimagequant/archive/refs/tags/4.4.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libimagequant.h"
    assert_path_exists lib/"libimagequant.dll.a"
    assert_path_exists bin/"libimagequant.dll"
  end
end
