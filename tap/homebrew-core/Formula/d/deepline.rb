class Deepline < Formula
  desc "CLI for Deepline data enrichment and durable plays"
  homepage "https://code.deepline.com"
  url "https://registry.npmjs.org/deepline/-/deepline-0.3.537.tgz"
  sha256 "916068ce87b193467cd06bfa539ca02549867a04e2a34096040f18a9638da990"
  license "MIT"

  livecheck do
    throttle days: 1
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "e9dcb83f527af2ad85e374777d96b73da947dd66deee638aec150b65a13cbfe8"
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
