class Libmaxminddb < Formula
  desc "C library for the MaxMind DB file format"
  homepage "https://maxmind.github.io/libmaxminddb/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libmaxminddb-1.12.2-2-any.pkg.tar.zst"
  sha256 "db42c7f901988cca02641595cd1416eb5d10a5c8e357fdfef01ba4723bdb2810"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/maxmind/libmaxminddb/releases/download/1.14.1/libmaxminddb-1.14.1.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/mmdblookup.exe", "mmdblookup"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mmdblookup --version 2>&1")
  end
end
