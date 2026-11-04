class Marked < Formula
  desc "Markdown parser and compiler built for speed"
  homepage "https://marked.js.org/"
  url "https://registry.npmjs.org/marked/-/marked-18.1.3.tgz"
  sha256 "5ca94887c584f7a67b27acd6b08e0a19da651418b627295d82d57fe63a56c82b"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b22a72c884285464c938c0da0867a9737ad1a59e065f84a779951db20e72d1c8"
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
