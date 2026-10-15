class Mysql < Formula
  desc "Open source relational database management system"
  homepage "https://github.com/mysql/mysql-server"
  url "https://cdn.mysql.com/Downloads/MySQL-26.7/mysql-26.7.0-winx64.zip"
  version "26.7.0"
  sha256 "e8d5b08f0d430555497679fa713a9649953ea067f952708a74cdcdf34f8e4b7d"
  license "GPL-2.0-only" => { with: "Universal-FOSS-exception-1.0" }

  # Official Oracle MySQL Windows x64 ZIP archive. No service is registered or started.

  conflicts_with "mariadb", because: "mariadb and mysql install the same binaries"

  def install
    install_tree
    %w[mysql mysqladmin mysqlbinlog mysqlcheck mysqld mysqldump mysqlimport mysqlshow mysqlslap
       mysql_config_editor mysql_tzinfo_to_sql].each do |cmd|
      windows_exe "libexec/bin/#{cmd}.exe", cmd
    end
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mysql --version")
    assert_match version.to_s, shell_output("#{bin}/mysqld --version")
  end
end
