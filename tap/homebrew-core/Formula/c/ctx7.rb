class Ctx7 < Formula
  desc "Manage AI coding skills and documentation context"
  homepage "https://context7.com"
  url "https://registry.npmjs.org/ctx7/-/ctx7-0.6.1.tgz"
  sha256 "9716fd758cbe4d6b9b182764c8caebb2beadcd27b63f90d304b6cbef91baf7fc"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b1d69c9d631fce5e05cb00d2f04b9993647984fc1a3bea3c79c3f24ee3e9855e"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ctx7 --version")
    assert_match "Not logged in", shell_output("#{bin}/ctx7 whoami")
    assert_match "No skills installed", shell_output("#{bin}/ctx7 skills list")
    system bin/"ctx7", "library", "react", "hooks"
  end
end
