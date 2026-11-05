class EasCli < Formula
  desc "Command-line tool for working with Expo Application Services"
  homepage "https://docs.expo.dev/eas/"
  url "https://registry.npmjs.org/eas-cli/-/eas-cli-27.4.1.tgz"
  sha256 "a2ab828f3607136c065198ef654a348b1d3db3010b6d18932ebcfae946e1bede"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8fff643760f2924cad4f06d419f8bbed8673ce1e2c6bd7db1cd36e4836ee2dc7"
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
