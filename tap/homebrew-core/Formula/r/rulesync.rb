class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.1.2.tgz"
  sha256 "36799481ae3f44ecd7023dbf79c1e2c6dda7d77cf5829c235f0e2dd20f56f145"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "5ae0d2e178bfcd9091a6af9d3993692e5467af4142719896b7a51fec27b1d661"
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
