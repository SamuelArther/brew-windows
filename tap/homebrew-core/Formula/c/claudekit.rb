class Claudekit < Formula
  desc "Intelligent guardrails and workflow automation for Claude Code"
  homepage "https://github.com/carlrannaberg/claudekit"
  url "https://registry.npmjs.org/claudekit/-/claudekit-0.9.5.tgz"
  sha256 "9059948267aeaf80b8d06805b7960fa508efbe6b446eb1ddda6e616987276c23"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "29fa919c2df0f404707f0bebbcc43ef30c06aa898c67bcf43fffd787fe3df853"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/claudekit --version")
    assert_match "Hooks:", shell_output("#{bin}/claudekit list")
    assert_match ".claudekit/config.json not found", shell_output("#{bin}/claudekit doctor 2>&1", 1)
  end
end
