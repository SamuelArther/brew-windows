class EasCli < Formula
  desc "Command-line tool for working with Expo Application Services"
  homepage "https://docs.expo.dev/eas/"
  url "https://registry.npmjs.org/eas-cli/-/eas-cli-27.10.0.tgz"
  sha256 "ea752ac4d1eed0d847396d42646f05269ee3e7a71521eea9e1c6f2a651312950"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "788cda103e0a24cb62018129b6657bee5afaf0a8c2df97fca6744670978b0c1f"
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
