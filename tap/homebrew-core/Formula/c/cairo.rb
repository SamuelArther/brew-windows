class Cairo < Formula
  desc "Vector graphics library with cross-device output support"
  homepage "https://cairographics.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cairo-1.18.6-2-any.pkg.tar.zst"
  sha256 "021c0a4d1df463d08d0cac170818a7cbaccaa5b09ac6edc7948e461cb5a46e85"
  license any_of: ["LGPL-2.1-only", "MPL-1.1"]
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://cairographics.org/releases/?C=M&O=D"
    regex(%r{href=(?:["']?|.*?/)cairo[._-]v?(\d+\.\d*[02468](?:\.\d+)*)\.t}i)
  end

  # The libraries cairo.pc and cairo-gobject.pc require (py3cairo's pkg-config lookup: "Package 'libpng',
  # required by 'cairo', not found"), as upstream's dependencies.
  depends_on "fontconfig"
  depends_on "freetype"
  depends_on "glib"
  depends_on "libpng"
  depends_on "pixman"

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"cairo/cairo.h"
    assert_path_exists lib/"libcairo-gobject.dll.a"
    assert_path_exists bin/"libcairo-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/cairo-dwrite-font.pc").read
  end
end
