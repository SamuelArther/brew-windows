class Lerna < Formula
  desc "Tool for managing JavaScript projects with multiple packages"
  homepage "https://lerna.js.org"
  url "https://registry.npmjs.org/lerna/-/lerna-10.1.3.tgz"
  sha256 "2ab0a784ddb4f70b2d8f4c1901fd564b3c3c5c3f872588a8a3c2fe3ad1dcd6d3"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d21c379910094dfb466dccaba261201f2529e6af3c1f1fec2a9b55709fc831a2"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lerna --version")

    output = shell_output("#{bin}/lerna init --independent 2>&1")
    assert_match "lerna success Initialized Lerna files", output
  end
end
