class Snowball < Formula
  desc "Stemming algorithms"
  homepage "https://snowballstem.org"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-snowball-3.1.1-1-any.pkg.tar.zst"
  sha256 "3625a953702483df80e666a28dde12a718053bf6b7c684c431af55fa01d41a9c"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/snowballstem/snowball/archive/refs/tags/v3.1.1.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/snowball.exe", "snowball"
    msys2_bin "ucrt64/bin/stemwords.exe", "stemwords"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/snowball --version 2>&1")
  end
end
