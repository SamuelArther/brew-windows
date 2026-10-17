class ReadwiseCli < Formula
  desc "Command-line interface for Readwise and Reader"
  homepage "https://readwise.io/cli"
  url "https://registry.npmjs.org/@readwise/cli/-/cli-0.5.10.tgz"
  sha256 "43d0be21396b40766c834bd671b5b5ecc79b27d20766d7c16ee6e064191a6a5e"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "906cbaf704a20ba309957adcacd87e5d0d04f1f1d118b9db483b948879c68e66"
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
