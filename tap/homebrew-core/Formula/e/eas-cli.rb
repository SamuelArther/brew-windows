class EasCli < Formula
  desc "Command-line tool for working with Expo Application Services"
  homepage "https://docs.expo.dev/eas/"
  url "https://registry.npmjs.org/eas-cli/-/eas-cli-27.9.0.tgz"
  sha256 "92e55b48f381c2f5898abe3c1a4eadb4d3778d90cac3332c228a279ee5e9a81c"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "022a157eb43ba9f6dd4f80fde2f198b9ea7acbf07c1e32adcd2aec6e0e1855fe"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/eas --version")
    assert_match "Run this command inside a project directory",
                 shell_output("#{bin}/eas diagnostics 2>&1", 1)
  end
end
