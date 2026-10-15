class Sqlite < Formula
  desc "Command-line interface for SQLite"
  homepage "https://sqlite.org/index.html"
  url "https://sqlite.org/2026/sqlite-tools-win-x64-3540000.zip"
  version "3.54.0"
  sha256 "63cb06e952a83fd140139b008ed5b47496608161c62f8cd17bc96e8beb1de029"
  license "blessing"

  def install
    bin.install "sqlite3.exe"
    bin.install "sqldiff.exe"
    bin.install "sqlite3_analyzer.exe"
    bin.install "sqlite3_rsync.exe"
  end

  test do
    path = testpath/"school.sql"
    path.write <<~SQL
      create table students (name text, age integer);
      insert into students (name, age) values ('Bob', 14);
      insert into students (name, age) values ('Sue', 12);
      insert into students (name, age) values ('Tim', 13);
      select name from students order by age asc;
    SQL

    names = shell_output("#{bin}/sqlite3 < #{path}").strip.split("\n")
    assert_equal %w[Sue Tim Bob], names
  end
end
