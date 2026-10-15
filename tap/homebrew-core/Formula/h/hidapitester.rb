class Hidapitester < Formula
  desc "Command-line tool to exercise USB HID devices via HIDAPI"
  homepage "https://github.com/todbot/hidapitester"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-hidapitester-0.5-1-any.pkg.tar.zst"
  sha256 "0ec2f7c2c4199c249473f4766295f596da5a98124daf896d9bf2c796673d9007"
  license "GPL-3.0-only"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/todbot/hidapitester/archive/refs/tags/v0.7.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/hidapitester.exe", "hidapitester"
  end

  test do
    assert_path_exists bin/"hidapitester.exe"
  end
end
