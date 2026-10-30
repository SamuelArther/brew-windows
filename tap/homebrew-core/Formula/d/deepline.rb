class Deepline < Formula
  desc "CLI for Deepline data enrichment and durable plays"
  homepage "https://code.deepline.com"
  url "https://registry.npmjs.org/deepline/-/deepline-0.3.440.tgz"
  sha256 "169abc20faa1a73c55a779d209ac6c23c63818047523d257b412f46b92c95dfe"
  license "MIT"

  livecheck do
    throttle 20
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "0709c126e0238159d36dfa1da50d18495d1d0ca90e78208a01cd90e3e7721d33"
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
