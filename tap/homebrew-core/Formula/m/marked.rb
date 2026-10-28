class Marked < Formula
  desc "Markdown parser and compiler built for speed"
  homepage "https://marked.js.org/"
  url "https://registry.npmjs.org/marked/-/marked-18.1.2.tgz"
  sha256 "9e3e0f5beb9c15a671025eb588c64dc8078a529121d1de809f8752bc422ad09d"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "7415d7504f458129e58260b4735b44d1e1525e0cc99f9dc36928cd31c0d647cb"
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
