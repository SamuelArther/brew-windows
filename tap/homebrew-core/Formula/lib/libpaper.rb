class Libpaper < Formula
  desc "Library for handling paper characteristics"
  homepage "https://github.com/rrthomas/libpaper"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libpaper-2.2.8-1-any.pkg.tar.zst"
  sha256 "8dde5e0faa07f652969769249c993092860fcaa8ba5aed9f68063eacc0721aa1"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/rrthomas/libpaper/releases/download/v2.2.8/libpaper-2.2.8.tar.gz"
    strategy :github_latest
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/paper.exe", "paper"
    msys2_bin "ucrt64/bin/paperconf.exe", "paperconf"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/paper --version 2>&1")
  end
end
