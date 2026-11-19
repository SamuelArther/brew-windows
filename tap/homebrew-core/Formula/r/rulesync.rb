class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.9.2.tgz"
  sha256 "18464183281ce1ef51c9ef9440445a27a384983e7224b6b8143f15b1d7d4fbc8"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "9d32c1e469accaaf324e5e046873d94b796418e050438d756c00537ff65846e3"
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
