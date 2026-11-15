class E2b < Formula
  desc "CLI to manage E2B sandboxes and templates"
  homepage "https://e2b.dev"
  url "https://registry.npmjs.org/@e2b/cli/-/cli-2.24.0.tgz"
  sha256 "e86f704e77a3ea586f453b97fbe99c979dc5385d7097b1a1c0dbd0ae31dc7939"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "259dc9420ce19275044cc02fd4f5d31a7db721296288c1db46617ea3f3e51986"
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
