class SqlFormatter < Formula
  desc "Whitespace formatter for different query languages"
  homepage "https://sql-formatter-org.github.io/sql-formatter/"
  url "https://registry.npmjs.org/sql-formatter/-/sql-formatter-15.9.0.tgz"
  sha256 "ca4475f0c03b22c8803dcd05f13c02961dd8363ae4f112b2a3d071edf5fa095f"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "84e42cfe4042582bc49029b9f2c4c4c7e3a74867a290c27bea5c241cfb37743d"
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
