class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-40.1.0.tgz"
  sha256 "9e46d8f4289c26b4d784bc84958c0efe633300b8690b779822e450caa761b61e"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8bd361c17be0f727c74105e5052f9d8b9bbe3bc826ff6dd24a651f4b50ef3371"
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
