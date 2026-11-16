class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.8.3.tgz"
  sha256 "902b4f48b3221923982b124327a3709e894f6c73734d8c1dcf8c39e8852b45ab"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d325f192aa5001d055ed2d6aecdd6e2bffc24ff2fec867a0a237e098e8955626"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rulesync --version")

    output = shell_output("#{bin}/rulesync init")
    assert_match "rulesync initialized successfully", output
    assert_match "Project overview and general development guidelines", (testpath/".rulesync/rules/overview.md").read
  end
end
