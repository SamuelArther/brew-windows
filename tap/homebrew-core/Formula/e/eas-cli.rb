class EasCli < Formula
  desc "Command-line tool for working with Expo Application Services"
  homepage "https://docs.expo.dev/eas/"
  url "https://registry.npmjs.org/eas-cli/-/eas-cli-25.3.0.tgz"
  sha256 "198dbcb08096ade15998b9a34a28479f86088e1fcfd89e9d213bdabac4966133"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b9dd9f04b2b0f3137bd2b8a5019b98285bf13c0f502e3dbca0bf67f91f91b612"
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
