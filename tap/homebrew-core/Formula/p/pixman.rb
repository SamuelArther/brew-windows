class Pixman < Formula
  desc "Low-level library for pixel manipulation"
  homepage "https://cairographics.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pixman-0.46.4-3-any.pkg.tar.zst"
  sha256 "4c01b986563d2b5f7ad6fcec2d0ec9915d2f242ddac5eacddc19971964241cc5"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://cairographics.org/releases/?C=M&O=D"
    regex(/href=.*?pixman[._-]v?(\d+\.\d*[02468](?:\.\d+)*)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"pixman-1/pixman.h"
    assert_path_exists lib/"libpixman-1.dll.a"
    assert_path_exists bin/"libpixman-1-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/pixman-1.pc").read
  end
end
