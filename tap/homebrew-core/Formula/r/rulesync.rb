class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.6.0.tgz"
  sha256 "301f119385a211a144c47d2042af76536d2d0bc1c8983abd66149ba6beaaee62"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "68f8ef04f0553a13f48c6728e7237028c7d35329aa25636fa685f24574618fe0"
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
