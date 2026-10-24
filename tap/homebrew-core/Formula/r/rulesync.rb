class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-40.2.0.tgz"
  sha256 "f938b8f02f2735bc531b3e124f698979dbc0062a3b82328657d9b7ecb9006fd4"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "5c4f87db1d2c5c3b061f26a5872267e528dfa1f44ba96925bf25cccb2dfb1f25"
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
