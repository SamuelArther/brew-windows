class Deepline < Formula
  desc "CLI for Deepline data enrichment and durable plays"
  homepage "https://code.deepline.com"
  url "https://registry.npmjs.org/deepline/-/deepline-0.3.400.tgz"
  sha256 "e9abcf7c73b56922a8d64dee59d1c4a4b2d4cee84fe398e83e7cf20dd32330f4"
  license "MIT"

  livecheck do
    throttle 20
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "2adf395b58cae93f34ce6ad4dde4e498d5e377db67d1ee04f0dbb60af1704f94"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match '"status": "not connected"',
      shell_output("#{bin}/deepline auth status --auth-scope folder --json")
  end
end
