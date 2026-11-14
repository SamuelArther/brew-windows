class Deepline < Formula
  desc "CLI for Deepline data enrichment and durable plays"
  homepage "https://code.deepline.com"
  url "https://registry.npmjs.org/deepline/-/deepline-0.3.505.tgz"
  sha256 "414d624dd8571af9349ce2f7c53b0e3fac7c27866e8043a2066ff5059065430c"
  license "MIT"

  livecheck do
    throttle days: 1
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "ad912f2e9be71e6e5af613bc03186df659fc30ab03afcb50d3eab0a5c586ca8a"
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
