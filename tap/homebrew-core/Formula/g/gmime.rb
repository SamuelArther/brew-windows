class Gmime < Formula
  desc "MIME mail utilities"
  homepage "https://github.com/jstedfast/gmime"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gmime-3.2.15-2-any.pkg.tar.zst"
  sha256 "55491697164cea6e4c9ea739d9378259b62df0b93207bba802a933743d098e44"
  license "LGPL-2.1-or-later"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/jstedfast/gmime/releases/download/3.2.15/gmime-3.2.15.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gmime-3.0/gmime/gmime.h"
    assert_path_exists lib/"libgmime-3.0.dll.a"
    assert_path_exists bin/"libgmime-3.0-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gmime-3.0.pc").read
  end
end
