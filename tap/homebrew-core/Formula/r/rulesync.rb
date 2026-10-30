class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.3.0.tgz"
  sha256 "d0d59796a2e863257568b3eed1d348f0792e250b531efa331c0f3273347350ef"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "9e3ebdea27ff049f6580cfcf5b40d5d2ff258c6301025055829d10c6c90851dc"
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
