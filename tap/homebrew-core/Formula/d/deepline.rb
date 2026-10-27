class Deepline < Formula
  desc "CLI for Deepline data enrichment and durable plays"
  homepage "https://code.deepline.com"
  url "https://registry.npmjs.org/deepline/-/deepline-0.3.420.tgz"
  sha256 "8619aa06f689a42f0b9fb656c6703679046acdfee74c42db9cfeb3fd79ba5728"
  license "MIT"

  livecheck do
    throttle 20
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "ac725b6578ef6227051d69e0c7dbc20b0467e062eede72d768f1bfd4baa34f8a"
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
