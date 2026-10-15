class Libdatrie < Formula
  desc "Double-Array Trie Library"
  homepage "https://github.com/tlwg/libdatrie"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libdatrie-0.2.14-1-any.pkg.tar.zst"
  sha256 "ea92777801ad325278193a362967540752ae37035016df2f6bcee2d89928a195"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/tlwg/libdatrie/releases/download/v0.2.14/libdatrie-0.2.14.tar.xz"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/trietool-0.2.exe", "trietool-0.2"
    msys2_bin "ucrt64/bin/trietool.exe", "trietool"
  end

  test do
    assert_path_exists bin/"trietool-0.2.exe"
  end
end
