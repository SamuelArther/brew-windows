class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.7.0.tgz"
  sha256 "812c7b2d7ec63baee776159d5e9a7a796439178ef2ac50193a2bdccc9bef4ca0"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4bd98bbde12ff5c3f0449f286b250af237d3fd608560869777f2fd751491d6f1"
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
