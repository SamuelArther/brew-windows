class PostgresqlAT17 < Formula
  desc "Object-relational database system"
  homepage "https://www.postgresql.org/"
  url "https://get.enterprisedb.com/postgresql/postgresql-17.11-1-windows-x64-binaries.zip"
  sha256 "6eabdf00d2893713b75db4336a23c3fdf505f056e217ec6e2e95d901750cfea3"
  license "PostgreSQL"

  # EDB's official PostgreSQL Windows binaries zip (linked from postgresql.org). No service is registered or started.

  def install
    install_tree
    %w[clusterdb createdb createuser dropdb dropuser ecpg initdb pg_amcheck pg_archivecleanup
       pg_basebackup pg_checksums pg_combinebackup pg_config pg_controldata pg_createsubscriber pg_ctl
       pg_dump pg_dumpall pg_isready pg_receivewal pg_recvlogical pg_resetwal pg_restore pg_rewind
       pg_test_fsync pg_test_timing pg_upgrade pg_waldump pg_walsummary pgbench postgres psql
       reindexdb vacuumdb].each do |cmd|
      windows_exe "libexec/bin/#{cmd}.exe", cmd
    end
  end

  test do
    system bin/"initdb", testpath/"test", "-U", "postgres", "-E", "UTF8"
    assert_match version.to_s, shell_output("#{bin}/psql --version")
  end
end
