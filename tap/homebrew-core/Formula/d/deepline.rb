class Deepline < Formula
  desc "CLI for Deepline data enrichment and durable plays"
  homepage "https://code.deepline.com"
  url "https://registry.npmjs.org/deepline/-/deepline-0.3.535.tgz"
  sha256 "4322952a70f7adf1255076f25c8ede6ea6fad4dcc07f8f30f5b4b697f15fc1b2"
  license "MIT"

  livecheck do
    throttle days: 1
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "01ae8a4c36d8e7815660b850f51a5d813dd8df6507cb7825a1330f0d8f90e1fe"
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
