class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-37.1.0.tgz"
  sha256 "d29ef0c6749d9c0a9259beadca892caf723245fd57022f206fed32d461c9fcad"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "93c8fdd8160a72976f735fa0fc8744faafcece7050ec8ae331eb224f5a9ae6c4"
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
