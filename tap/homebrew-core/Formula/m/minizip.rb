class Minizip < Formula
  desc "C library for zip/unzip via zLib"
  homepage "https://www.winimage.com/zLibDll/minizip.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-minizip-1.3.2-2-any.pkg.tar.zst"
  sha256 "607cfd490cd9c47db7381663963d8c7b0152d7b487a391fb3bbc3f09b6e50967"
  license "Zlib"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://zlib.net/zlib-1.3.2.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-bzip2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-bzip2-1.0.8-4-any.pkg.tar.zst"
    sha256 "f03a2174034ddd2d96cecd34f617c5f8e2ef86c812b8d2bb3b8875257f2c8bfa"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/miniunzip.exe", "miniunzip"
    msys2_bin "ucrt64/bin/minizip.exe", "minizip"
  end

  test do
    system bin/"minizip", "--version"
  end
end
