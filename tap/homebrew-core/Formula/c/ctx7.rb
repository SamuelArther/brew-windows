class Ctx7 < Formula
  desc "Manage AI coding skills and documentation context"
  homepage "https://context7.com"
  url "https://registry.npmjs.org/ctx7/-/ctx7-0.8.1.tgz"
  sha256 "ef12741d34ed58bc4567239da4642cc699d8df768c9ac68c700c6386395804f4"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2a2a5cb3c0a92c4f10e885c7b0470adb982502088dacdc1cbe9ec5d6b48ee5af"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ctx7 --version")
    assert_match "Not logged in", shell_output("#{bin}/ctx7 whoami")
    system bin/"ctx7", "library", "react", "hooks"
  end
end
