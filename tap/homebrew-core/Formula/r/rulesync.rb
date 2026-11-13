class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.8.1.tgz"
  sha256 "afeb1cf0b687a91bf29abc139a2ab74b3311e784e4f1b458a93a7fe3be6fd1ba"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a731ecd5fcc50259acb11d24061a2291ce24c51de745902355a90795c9dd0d8b"
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
