class Mariadb < Formula
  desc "Drop-in replacement for MySQL"
  homepage "https://mariadb.org/"
  url "https://archive.mariadb.org/mariadb-13.0.2/winx64-packages/mariadb-13.0.2-winx64.zip"
  version "13.0.2"
  sha256 "ba9cefd6674b3f2f7bc67334c05305dc7c919643d76836e10767e70db0b5fb74"
  license "GPL-2.0-only"
  revision 1

  # Official MariaDB Windows x64 ZIP package. No service is registered or started.

  conflicts_with "mysql", because: "mariadb, mysql, and percona install the same binaries"

  def install
    install_tree
    %w[mariadb mariadbd mariadb-admin mariadb-binlog mariadb-check mariadb-dump mariadb-import
       mariadb-install-db mariadb-show mariadb-slap mariadb-upgrade mysql mysqld mysqladmin mysqldump].each do |cmd|
      windows_exe "libexec/bin/#{cmd}.exe", cmd
    end
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mariadb --version")
  end
end
