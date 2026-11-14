class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.8.2.tgz"
  sha256 "340e182f251572241db0868d2e717d8f6caafbed3b02bea9647753fd36533491"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "ea243e861f66c441c0b2d5405e7b5bbf6db3e38f9e0fa6c5107ccb1edebcbf24"
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
