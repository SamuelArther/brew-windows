class Deepline < Formula
  desc "CLI for Deepline data enrichment and durable plays"
  homepage "https://code.deepline.com"
  url "https://registry.npmjs.org/deepline/-/deepline-0.3.320.tgz"
  sha256 "f682ab8737397a27080f3593ee9a063abf311c2460b186c139b529877007dc74"
  license "MIT"

  livecheck do
    throttle 20
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "ec4f3b940735dc808660c63e84630b4832f97d34c6a379544fd60cbab819a834"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match '"status": "not connected"',
      shell_output("#{bin}/deepline auth status --auth-scope folder")
  end
end
