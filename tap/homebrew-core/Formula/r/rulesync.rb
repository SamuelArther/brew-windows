class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-41.0.0.tgz"
  sha256 "a3db2475176e4b314574b0b3f786a0976ec109b459c9ffc2ddaab4b3197d5a30"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "71fd0666f61568612b35495550df83829b3a11a042a3eb4ed2f1db06d55e11fe"
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
