class Zydis < Formula
  desc "Fast and lightweight x86/x86_64 disassembler library"
  homepage "https://zydis.re"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zydis-4.1.1-1-any.pkg.tar.zst"
  sha256 "87bb07c0578bc3565addfeca0c822a5a5a47ca85e0278c6fc1ad9b95213b8612"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/zyantific/zydis/archive/refs/tags/v4.1.1.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-zycore-c" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zycore-c-1.5.2-2-any.pkg.tar.zst"
    sha256 "3038019a9cf586c07b35826c11c69c35c68d0fec696003d496245950e7214f64"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/ZydisDisasm.exe", "ZydisDisasm"
    msys2_bin "ucrt64/bin/ZydisInfo.exe", "ZydisInfo"
  end

  test do
    assert_path_exists bin/"ZydisDisasm.exe"
  end
end
