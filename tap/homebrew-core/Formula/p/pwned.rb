class Pwned < Formula
  desc "CLI for the 'Have I been pwned?' service"
  homepage "https://github.com/wKovacs64/pwned"
  url "https://registry.npmjs.org/pwned/-/pwned-14.0.0.tgz"
  sha256 "ff28cbf472891e333a7ba2b9ed499a0d1f8814b199d015a5c5ee6ec6a6917349"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e29088808e0dce070b069e1106eedb7a01882b787be568154e2b518ebf207a1c"
  end

  depends_on "node"

  conflicts_with "bash-snippets", because: "both install `pwned` binaries"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pwned --version")

    assert_match "Oh no — pwned", shell_output("#{bin}/pwned pw homebrew 2>&1")
  end
end
