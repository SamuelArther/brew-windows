class Marked < Formula
  desc "Markdown parser and compiler built for speed"
  homepage "https://marked.js.org/"
  url "https://registry.npmjs.org/marked/-/marked-18.1.1.tgz"
  sha256 "c05b3cff890d35e1208805a01fd0501571d09a81e725de0559b10bdfae90adad"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "247a5a4378e496692b7f12fa3b839c70357ac4f44bb8b21f079236a085f87c09"
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
