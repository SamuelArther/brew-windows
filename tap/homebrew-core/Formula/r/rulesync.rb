class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.2.0.tgz"
  sha256 "29928e5aa8a017f4ac5823467ef496235756398960228c0b11b676b2408df00c"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3c3b0f9c2e061cd09d9a3fd65064886a9104eaa415f0911eb8cafbc34db86d8e"
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
