class EasCli < Formula
  desc "Command-line tool for working with Expo Application Services"
  homepage "https://docs.expo.dev/eas/"
  url "https://registry.npmjs.org/eas-cli/-/eas-cli-27.11.0.tgz"
  sha256 "b4d159a7c202a0ddf9e96f63f9cc8a571811f63f341cefa0f0194881526a2be8"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "c034536ace5bfd8f15f305ac41fc4f482d4542c36706b7662ffefc39d294f6b5"
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
