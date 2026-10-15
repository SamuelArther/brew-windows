class WebpPixbufLoader < Formula
  desc "WebP Image format GdkPixbuf loader"
  homepage "https://github.com/aruiz/webp-pixbuf-loader"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-webp-pixbuf-loader-0.2.7-2-any.pkg.tar.zst"
  sha256 "04481f1881d2122b8eb3c1cc5f23177a349ff22f8dc8928fc55878115235bb8c"
  license "LGPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/aruiz/webp-pixbuf-loader/archive/refs/tags/0.2.7.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists lib/"gdk-pixbuf-2.0/2.10.0/loaders/libpixbufloader-webp.dll"
  end
end
