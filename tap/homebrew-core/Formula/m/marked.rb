class Marked < Formula
  desc "Markdown parser and compiler built for speed"
  homepage "https://marked.js.org/"
  url "https://registry.npmjs.org/marked/-/marked-18.2.1.tgz"
  sha256 "d66c5213fc203466186060105b01f51896764ba64b3797d2620d02e28e34b8ca"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "95a5af79b99ea11edbfd17c5615ef747ba0d660d1a87450b8443d38faa017c4a"
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
