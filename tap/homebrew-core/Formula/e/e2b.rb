class E2b < Formula
  desc "CLI to manage E2B sandboxes and templates"
  homepage "https://e2b.dev"
  url "https://registry.npmjs.org/@e2b/cli/-/cli-2.22.3.tgz"
  sha256 "ad46571fb8696bcdf6df385802db4f3ddcb847d30b548c1a26bf90cc29167077"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "04a2b8b319d5af968b720ec7333b1a3b619533182de5a0a667e89ee9d2a69420"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/e2b --version")
    assert_match "Not logged in", shell_output("#{bin}/e2b auth info")
  end
end
