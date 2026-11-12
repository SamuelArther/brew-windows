class Retire < Formula
  desc "Scanner detecting the use of JavaScript libraries with known vulnerabilities"
  homepage "https://retirejs.github.io/retire.js/"
  url "https://registry.npmjs.org/retire/-/retire-6.1.1.tgz"
  sha256 "810b0c9099d8d1d8864549192e80bff5cd868f1ea0cbe4b2d3eb9a10d68d99de"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3fadd2d72ad7fd57b56907045e652fdfe1b893a9322a8f1d02e1ae042eabda68"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/retire --version")

    system "git", "clone", "https://github.com/appsecco/dvna.git"
    output = shell_output("#{bin}/retire --path dvna 2>&1", 13)
    assert_match(/jquery (\d+(?:\.\d+)+) has known vulnerabilities/, output)
  end
end
