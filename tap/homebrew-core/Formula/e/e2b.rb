class E2b < Formula
  desc "CLI to manage E2B sandboxes and templates"
  homepage "https://e2b.dev"
  url "https://registry.npmjs.org/@e2b/cli/-/cli-2.25.0.tgz"
  sha256 "867a3bc6343025759dc050dd5b439f6039980e93b7f5023c9cace7a955c808d8"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6518c309938ea2b68354f8b757ee81f2b70be7f57f1c8e70586255482202d438"
  end

  depends_on "node"

  allow_network_access! :build

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/e2b --version")
    assert_match "Not logged in", shell_output("#{bin}/e2b auth info")
  end
end
