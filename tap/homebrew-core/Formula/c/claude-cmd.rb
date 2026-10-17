class ClaudeCmd < Formula
  desc "Claude Code Commands Manager"
  homepage "https://github.com/kiliczsh/claude-cmd"
  url "https://registry.npmjs.org/claude-cmd/-/claude-cmd-1.1.1.tgz"
  sha256 "c6b990f7c55ec1281dca603b284d55b468ca7bbdfe217fc8091f5a8f85f16367"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "40cac9a047e98e69f9ace0fb9c9d0f513c78d87f7b1a845bd4cba136152797c5"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    output = shell_output("#{bin}/claude-cmd list")
    assert_match "No commands installed yet", output
  end
end
