class Marked < Formula
  desc "Markdown parser and compiler built for speed"
  homepage "https://marked.js.org/"
  url "https://registry.npmjs.org/marked/-/marked-18.2.0.tgz"
  sha256 "f8f191f70f642128653254bf4b511e7fd7d77962b4c2a186d5f192e47f93d800"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f73eba76bb2f6ede5b6f8d27e962ef77748678afcb85b29983934b4a082d25ce"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_equal "<p>hello <em>world</em></p>", shell_output("#{bin}/marked -s 'hello *world*'").strip
  end
end
