class Xmake < Formula
  desc "Cross-platform build utility based on Lua"
  homepage "https://xmake.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-xmake-3.1.1-2-any.pkg.tar.zst"
  sha256 "5695c3b015acf2b025f3a410f73146ee33beb74a0df0a00999e6504b9b43933c"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/xmake-io/xmake/releases/download/v3.1.1/xmake-v3.1.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    assert_path_exists bin/"xmake"
  end
end
