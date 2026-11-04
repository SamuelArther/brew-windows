class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.5.0.tgz"
  sha256 "6713758e60f2937f5ae9e70d358acded9553c2ad340ab4fa6a417dcdd9f519d1"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "58c61898a32cc8d33e7eb4fc4aaf7aad0b4bf778e5e74fdd6dc81ebd3049472e"
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
