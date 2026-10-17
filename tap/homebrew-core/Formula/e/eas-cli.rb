class EasCli < Formula
  desc "Command-line tool for working with Expo Application Services"
  homepage "https://docs.expo.dev/eas/"
  url "https://registry.npmjs.org/eas-cli/-/eas-cli-25.0.0.tgz"
  sha256 "f9b92df7d3077c923e6d88d243daaad6690c7fa2dddf0befa8f884d3bdefae90"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4e3f4a9ca06975e09450dd35734b1f7762a6de8cc500750b83d65977f0280f8a"
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
