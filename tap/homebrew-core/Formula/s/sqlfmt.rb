class Sqlfmt < Formula
  desc "SQL formatter with width-aware output"
  homepage "https://sqlfum.pt/"
  url "https://github.com/madelynnblue/sqlfmt/releases/download/v0.5.0/sqlfmt_Windows_x86_64.zip"
  sha256 "7ee1c92507821f06f177b0a8243b4c690bd8cb2cf0cb257fedc14350e485ce03"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/madelynnblue/sqlfmt/archive/refs/tags/v0.5.0.tar.gz"
  end

  def install
    bin.install "sqlfmt.exe"
  end

  test do
    test_sql = "\"SELECT count(ID) AS count, foo FROM brewtest GROUP BY foo;\""
    assert_equal <<~SQL, shell_output("#{bin}/sqlfmt --print-width 40 --stmt #{test_sql}")
      SELECT
      \tcount(id) AS count, foo
      FROM
      \tbrewtest
      GROUP BY
      \tfoo;
    SQL

    assert_match version.to_s, shell_output("#{bin}/sqlfmt --version")
  end
end
