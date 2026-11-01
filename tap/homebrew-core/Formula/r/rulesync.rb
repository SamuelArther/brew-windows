class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.4.1.tgz"
  sha256 "a45a7cb3ceeaa883fb2073fac29bc396ce3149e1e745a4c415780d7030a40d81"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e5ef23ca4e6f2b818ceb2f416952fd46e4945bd9133d122553b72dc3c5e58f25"
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
