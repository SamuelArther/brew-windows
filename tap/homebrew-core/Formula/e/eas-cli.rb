class EasCli < Formula
  desc "Command-line tool for working with Expo Application Services"
  homepage "https://docs.expo.dev/eas/"
  url "https://registry.npmjs.org/eas-cli/-/eas-cli-27.5.0.tgz"
  sha256 "4abb8bc6d0ad969cabdf7c45d6b2d47a33df3d9e74d59ba9e65a13a0eff1256a"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3d858c107cce43ed44f3d94d47ee888d7f54f69768e62fd42cb68f21be4929e4"
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
