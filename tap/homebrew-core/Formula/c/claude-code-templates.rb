class ClaudeCodeTemplates < Formula
  desc "CLI tool for configuring and monitoring Claude Code"
  homepage "https://www.aitmpl.com/agents"
  url "https://registry.npmjs.org/claude-code-templates/-/claude-code-templates-1.30.0.tgz"
  sha256 "46a920e5380ac2e871df4c8c6d121330ddc22c2c3ec483f158c78462f1d58510"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "1335b1104e61069ce2d07d4624d6b1ab3ed697b4de079b231aa6d4354360a8e4"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args(ignore_scripts: false)
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cct --version")

    output = shell_output("#{bin}/cct --command testing/generate-tests --yes")
    assert_match "Installed 1 of 1", output
    assert_path_exists testpath/".claude/commands/generate-tests.md"
  end
end
