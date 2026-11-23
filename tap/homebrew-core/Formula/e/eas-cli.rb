class EasCli < Formula
  desc "Command-line tool for working with Expo Application Services"
  homepage "https://docs.expo.dev/eas/"
  url "https://registry.npmjs.org/eas-cli/-/eas-cli-27.11.0.tgz"
  sha256 "b4d159a7c202a0ddf9e96f63f9cc8a571811f63f341cefa0f0194881526a2be8"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "5151d558a9d75003b5d61fd52bfd558e2c4408c19d4724e914d339250d31ca59"
  end

  depends_on "node"

  allow_network_access! :build

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
