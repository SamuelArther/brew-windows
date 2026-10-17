class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-36.0.2.tgz"
  sha256 "eb910e5199cc4ec9ef02342099c1dbfa2b2781a6a59c62bd5f0ef9f9c59b4d68"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "9c6f4c86d45fb23a7e93ac3f2cf489bd94d6e3cd4a0ea622502ec9f911b03817"
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
