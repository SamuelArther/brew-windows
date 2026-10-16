class Civetweb < Formula
  desc "C/C++ embeddable web server with optional CGI, SSL and Lua support"
  homepage "https://github.com/civetweb/civetweb"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-civetweb-1.16-4-any.pkg.tar.zst"
  sha256 "580bb7b9cd1a708172081b8b1ab74c80e148a658ac00491ac81fb8f82a825366"
  license "MIT"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/civetweb/civetweb/archive/refs/tags/v1.16.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/civetweb.exe", "civetweb"
  end

  test do
    assert_path_exists bin/"civetweb.exe"
  end
end
