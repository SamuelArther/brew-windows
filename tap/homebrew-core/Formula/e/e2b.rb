class E2b < Formula
  desc "CLI to manage E2B sandboxes and templates"
  homepage "https://e2b.dev"
  url "https://registry.npmjs.org/@e2b/cli/-/cli-2.23.0.tgz"
  sha256 "dbc0e9e5eb9b326eac04193123aa2af16d54daa8e18ceef31e5f53d821411722"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6094e76432b9321189f316c8021646e9c05cf7a38f0da0c774f6975c4d4f238e"
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
