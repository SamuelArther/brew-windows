class Sqruff < Formula
  desc "Fast SQL formatter/linter"
  homepage "https://github.com/quarylabs/sqruff"
  url "https://github.com/quarylabs/sqruff/releases/download/v0.41.0/sqruff-windows-x86_64.zip"
  sha256 "cf7ffc77a9ce8fb0eeac62b0b43b2adf7733fa42dc49667ebdd25525decaac18"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/quarylabs/sqruff/archive/refs/tags/v0.41.0.tar.gz"
  end

  def install
    bin.install "sqruff.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sqruff --version")

    assert_match "AL01:	[aliasing.table]", shell_output("#{bin}/sqruff rules")

    (testpath/"test.sql").write <<~SQL
      SELECT * FROM user JOIN order ON user.id = order.user_id;
    SQL

    output = shell_output("#{bin}/sqruff lint --format human #{testpath}/test.sql 2>&1")
    assert_match "All Finished", output
  end
end
