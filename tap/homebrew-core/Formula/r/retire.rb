class Retire < Formula
  desc "Scanner detecting the use of JavaScript libraries with known vulnerabilities"
  homepage "https://retirejs.github.io/retire.js/"
  url "https://registry.npmjs.org/retire/-/retire-6.1.2.tgz"
  sha256 "5f386e2189098a7db476d460f6e5d5c83888cc97700bbda913cc7a632ed00d89"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "434e5d38e6196564ac1bbab58702f556d5c978c8821d06e613063b45d903878c"
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
