class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.11.0.tgz"
  sha256 "77fb87e7a2c983692a99fa93009ff129fb2056819601a895c20f0a4ec1a78f27"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3c0c838fbbbeb33a20ccefc4cb8747d929eb1614c87f591d542b1584dd678cb2"
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
