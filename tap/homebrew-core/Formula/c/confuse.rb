class Confuse < Formula
  desc "Configuration file parser library written in C"
  homepage "https://www.nongnu.org/confuse/manual/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-confuse-3.3-4-any.pkg.tar.zst"
  sha256 "61eadbc01c88af9f588e1e4e78cf85e1430e52a31ad5f06d2fe48c9a14e880e1"
  license "ISC"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libconfuse/libconfuse/releases/download/v3.4/confuse-3.4.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"confuse.h"
    assert_path_exists lib/"libconfuse.dll.a"
    assert_path_exists bin/"libconfuse-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libconfuse.pc").read
  end
end
