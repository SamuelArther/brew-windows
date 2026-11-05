class Deepline < Formula
  desc "CLI for Deepline data enrichment and durable plays"
  homepage "https://code.deepline.com"
  url "https://registry.npmjs.org/deepline/-/deepline-0.3.463.tgz"
  sha256 "c8aee97cd9e4a5f85ae39f45513e1c393240d47ca2f884b042dc38bc6a8bc60a"
  license "MIT"

  livecheck do
    throttle days: 1
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "8a0724481bb13de15504369abf06ca2213ca4485d289e948fdc2c49c87ac360d"
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
