class YelpTools < Formula
  desc "Tools that help create and edit Mallard or DocBook documentation"
  homepage "https://gitlab.gnome.org/GNOME/yelp-tools"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-yelp-tools-42.1-2-any.pkg.tar.zst"
  sha256 "a7d699e35818298891a3fd21d2c247f39776cdb3c7cfcfcc560f81ae2e53d608"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/yelp-tools/42/yelp-tools-42.1.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    assert_path_exists bin/"yelp-build"
  end
end
