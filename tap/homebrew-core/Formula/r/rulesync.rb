class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.12.0.tgz"
  sha256 "c0cf7f9f89961b2d9324bef7eddb2234e4b9a5bc35b0579756d86658560f53fc"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8c82a0ab19ba3201ff42cfc2111cff1d3f756c16a770707365bf610b4588922d"
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
