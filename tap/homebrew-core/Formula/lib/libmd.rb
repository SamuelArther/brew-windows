class Libmd < Formula
  desc "Message Digest functions from BSD systems"
  homepage "https://www.hadrons.org/software/libmd/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libmd-1.2.0-1-any.pkg.tar.zst"
  sha256 "e9677ae79de5444dd2f042fccb22502863c43e9dc8aa5455e04de9951dc9dd8c"
  license all_of: ["BSD-3-Clause", "BSD-2-Clause", "ISC", "Beerware", :public_domain]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://archive.hadrons.org/software/libmd/"
    regex(/href=.*?libmd[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"md2.h"
    assert_path_exists lib/"libmd.dll.a"
    assert_path_exists bin/"libmd-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libmd.pc").read
  end
end
