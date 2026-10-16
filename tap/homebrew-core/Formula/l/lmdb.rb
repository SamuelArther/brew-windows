class Lmdb < Formula
  desc "Lightning memory-mapped database: key-value data store"
  homepage "https://www.symas.com/lmdb.php"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lmdb-0.9.33-1-any.pkg.tar.zst"
  sha256 "4258380243d9530bf7754c7e2ad1a0e7a291f595be6beef26429757b5c8e7764"
  license "OLDAP-2.8"
  version_scheme 1

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://git.openldap.org/openldap/openldap/-/archive/LMDB_1.0.2/openldap-LMDB_1.0.2.tar.bz2"
    regex(/^LMDB[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/mdb_copy.exe", "mdb_copy"
    msys2_bin "ucrt64/bin/mdb_dump.exe", "mdb_dump"
    msys2_bin "ucrt64/bin/mdb_load.exe", "mdb_load"
    msys2_bin "ucrt64/bin/mdb_stat.exe", "mdb_stat"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mdb_copy -V 2>&1")
  end
end
