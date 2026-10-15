class Duckdb < Formula
  desc "Embeddable SQL OLAP Database Management System"
  homepage "https://www.duckdb.org"
  version "1.5.6"
  url "https://github.com/duckdb/duckdb/releases/download/v1.5.6/duckdb_cli-windows-amd64.zip"
  sha256 "798eae475d07c645ff3b914f7b0e676d06502b5412dd7552638fafed81f9e916"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "duckdb.exe"
  end

  test do
    path = testpath/"weather.sql"
    path.write <<~SQL
      CREATE TABLE weather (temp INTEGER);
      INSERT INTO weather (temp) VALUES (40), (45), (50);
      SELECT AVG(temp) FROM weather;
    SQL

    assert_match "45.0", shell_output("#{bin}/duckdb < #{path} 2>&1")
  end
end
