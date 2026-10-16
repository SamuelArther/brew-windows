class Irrlicht < Formula
  desc "Realtime 3D engine"
  homepage "https://irrlicht.sourceforge.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-irrlicht-1.8.5-4-any.pkg.tar.zst"
  sha256 "5a5f44a1bc9fdfb99b2f9abc806311f29a6d8f6c6491a7408bb6378ed79b0f4e"
  license "Zlib"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/irrlicht/Irrlicht%20SDK/1.8/1.8.5/irrlicht-1.8.5.zip"
    regex(%r{url=.*?/irrlicht[._-]v?(\d+(?:\.\d+)+)\.(?:t|zip)}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"irrlicht/path.h"
    assert_path_exists lib/"libirrlicht.dll.a"
    assert_path_exists bin/"irrlicht.dll"
  end
end
