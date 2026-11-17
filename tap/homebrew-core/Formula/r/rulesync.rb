class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.9.0.tgz"
  sha256 "9f248a84a314319c5afc2295a32d8223d8dc83a76308dc2e640c587fa769ec0a"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4c73da3180f88cebe8630a0f8b83fa2b7bbf86f78f5cdb3f33860f6573a33005"
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
