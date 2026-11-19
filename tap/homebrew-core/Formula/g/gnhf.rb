class Gnhf < Formula
  desc "Autonomous agent orchestrator for long-running coding tasks"
  homepage "https://github.com/kunchenguid/gnhf"
  url "https://registry.npmjs.org/gnhf/-/gnhf-0.1.53.tgz"
  sha256 "80a4d52c177c9512a797fb307d01b211dcb2510fb625557f386b07a3cd510e80"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "61d4de2a2e1b3bfa621e7174f21d52b4c724844d381fc78b28a88abf88adce05"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gnhf --version")

    output = shell_output("#{bin}/gnhf --current-branch 2>&1", 1)
    assert_match "gnhf: This command must be run inside a Git repository", output
  end
end
