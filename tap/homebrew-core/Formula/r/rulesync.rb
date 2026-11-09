class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.8.0.tgz"
  sha256 "203e6e8892192049078756737773ba83f44986c38ed08b08129dcb665d443097"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "db84a2cd326366e72ecaff51e21cd510b15bee58c5168665333742a2eb592313"
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
