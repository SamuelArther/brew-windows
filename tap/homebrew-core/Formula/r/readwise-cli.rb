class ReadwiseCli < Formula
  desc "Command-line interface for Readwise and Reader"
  homepage "https://readwise.io/cli"
  url "https://registry.npmjs.org/@readwise/cli/-/cli-0.5.11.tgz"
  sha256 "d1d3fca556693813e6d38acab1346340625537d3c76642ef5d53dd4e6f8c390c"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6f2f6f43095b801e5ffbc6ac534993232deebcae58c619aa74d0036ae67d975c"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/readwise --version")

    system bin/"readwise", "config", "set", "readonly", "true"
    assert_match "true", shell_output("#{bin}/readwise config get readonly")
  end
end
