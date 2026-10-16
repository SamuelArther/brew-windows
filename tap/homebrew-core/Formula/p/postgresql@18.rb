class PostgresqlAT18 < Formula
  desc "Object-relational database system"
  homepage "https://www.postgresql.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-postgresql-18.6-4-any.pkg.tar.zst"
  sha256 "45410374cb165108094a49a7d660e57963b804139b4d9ee45a892fc9da09273f"
  license "PostgreSQL"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://ftp.postgresql.org/pub/source/"
    regex(%r{href=["']?v?(18(?:\.\d+)+)/?["' >]}i)
  end

  keg_only :versioned_formula

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-gettext-runtime" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-runtime-1.0-1-any.pkg.tar.zst"
    sha256 "ba693dda4ac375af76ce481ff3a6e7481286546cc7dc6d56c7021dae34084157"
  end

  resource "mingw-w64-ucrt-x86_64-icu" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-icu-78.3-4-any.pkg.tar.zst"
    sha256 "db9282bbd0a2e74c31d66af6bd89eb68d647a09f721eef4c661768588dca4324"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-libxml2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libxml2-2.15.4-1-any.pkg.tar.zst"
    sha256 "ea6b8e7ee24d8674e719f1f0a1b3747a72158ab786dfc5392ccb9f8edd09582a"
  end

  resource "mingw-w64-ucrt-x86_64-lz4" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lz4-1.10.0-1-any.pkg.tar.zst"
    sha256 "00a807df81a4b7e82445178b8625962ed592cf1f0a08a8e04c8b9338c4362638"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  resource "mingw-w64-ucrt-x86_64-readline" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-readline-8.3.003-1-any.pkg.tar.zst"
    sha256 "de2423c2e10fcd88272a0ab2f833f6a082cfe613d4c17c2f548cc50a5d2190c4"
  end

  resource "mingw-w64-ucrt-x86_64-termcap" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-termcap-1.3.1-8-any.pkg.tar.zst"
    sha256 "c7891143e252568d8d46df72b3aefea932f4166771e17f22317218d7dd2af23c"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  resource "mingw-w64-ucrt-x86_64-zstd" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zstd-1.5.7-2-any.pkg.tar.zst"
    sha256 "dbdb8427280046a2b41697780aa4c52983b708082b0da4755951dc3bea96ca89"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/clusterdb.exe", "clusterdb"
    msys2_bin "ucrt64/bin/createdb.exe", "createdb"
    msys2_bin "ucrt64/bin/createuser.exe", "createuser"
    msys2_bin "ucrt64/bin/dropdb.exe", "dropdb"
    msys2_bin "ucrt64/bin/dropuser.exe", "dropuser"
    msys2_bin "ucrt64/bin/ecpg.exe", "ecpg"
    msys2_bin "ucrt64/bin/initdb.exe", "initdb"
    msys2_bin "ucrt64/bin/oid2name.exe", "oid2name"
    msys2_bin "ucrt64/bin/pg_amcheck.exe", "pg_amcheck"
    msys2_bin "ucrt64/bin/pg_archivecleanup.exe", "pg_archivecleanup"
    msys2_bin "ucrt64/bin/pg_basebackup.exe", "pg_basebackup"
    msys2_bin "ucrt64/bin/pg_checksums.exe", "pg_checksums"
    msys2_bin "ucrt64/bin/pg_combinebackup.exe", "pg_combinebackup"
    msys2_bin "ucrt64/bin/pg_config.exe", "pg_config"
    msys2_bin "ucrt64/bin/pg_controldata.exe", "pg_controldata"
    msys2_bin "ucrt64/bin/pg_createsubscriber.exe", "pg_createsubscriber"
    msys2_bin "ucrt64/bin/pg_ctl.exe", "pg_ctl"
    msys2_bin "ucrt64/bin/pg_dump.exe", "pg_dump"
    msys2_bin "ucrt64/bin/pg_dumpall.exe", "pg_dumpall"
    msys2_bin "ucrt64/bin/pg_isready.exe", "pg_isready"
    msys2_bin "ucrt64/bin/pg_receivewal.exe", "pg_receivewal"
    msys2_bin "ucrt64/bin/pg_recvlogical.exe", "pg_recvlogical"
    msys2_bin "ucrt64/bin/pg_resetwal.exe", "pg_resetwal"
    msys2_bin "ucrt64/bin/pg_restore.exe", "pg_restore"
    msys2_bin "ucrt64/bin/pg_rewind.exe", "pg_rewind"
    msys2_bin "ucrt64/bin/pg_test_fsync.exe", "pg_test_fsync"
    msys2_bin "ucrt64/bin/pg_test_timing.exe", "pg_test_timing"
    msys2_bin "ucrt64/bin/pg_upgrade.exe", "pg_upgrade"
    msys2_bin "ucrt64/bin/pg_verifybackup.exe", "pg_verifybackup"
    msys2_bin "ucrt64/bin/pg_waldump.exe", "pg_waldump"
    msys2_bin "ucrt64/bin/pg_walsummary.exe", "pg_walsummary"
    msys2_bin "ucrt64/bin/pgbench.exe", "pgbench"
    msys2_bin "ucrt64/bin/postgres.exe", "postgres"
    msys2_bin "ucrt64/bin/psql.exe", "psql"
    msys2_bin "ucrt64/bin/reindexdb.exe", "reindexdb"
    msys2_bin "ucrt64/bin/test_cloexec.exe", "test_cloexec"
    msys2_bin "ucrt64/bin/vacuumdb.exe", "vacuumdb"
    msys2_bin "ucrt64/bin/vacuumlo.exe", "vacuumlo"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/clusterdb --version 2>&1")
  end
end
