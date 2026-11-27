class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.16.0.tgz"
  sha256 "e4158f5259ffd9e19fa22573190b64b7c505ddb69f965f63c84e3cbb78411359"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3ed11e0ab7a1419244d80b140c0d52be14035a98903247ae13717e5c8ba4b28c"
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
