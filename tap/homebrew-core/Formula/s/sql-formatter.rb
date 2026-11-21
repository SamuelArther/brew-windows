class SqlFormatter < Formula
  desc "Whitespace formatter for different query languages"
  homepage "https://sql-formatter-org.github.io/sql-formatter/"
  url "https://registry.npmjs.org/sql-formatter/-/sql-formatter-15.10.0.tgz"
  sha256 "905eb4fbd22a65af0935ae00f7fc6d6713e36fdd96ba0f0d27a3774ddddc46e2"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f34dac61b8f7e429fcf2ce780b7fa0a9f6f1f7a479b9bf5c52ffb513856a2017"
  end

  depends_on "node"

  def install
    ENV["NPM_CONFIG_FORCE"] = "1"
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sql-formatter --version")

    (testpath/"test.sql").write <<~SQL
      SELECT * FROM users WHERE id = 1;
    SQL

    system bin/"sql-formatter", "--fix", "test.sql"
    expected_output = <<~SQL
      SELECT
        *
      FROM
        users
      WHERE
        id = 1;
    SQL

    assert_equal expected_output, (testpath/"test.sql").read
  end
end
