class JsBeautify < Formula
  desc "JavaScript, CSS and HTML unobfuscator and beautifier"
  homepage "https://beautifier.io"
  url "https://registry.npmjs.org/js-beautify/-/js-beautify-2.0.3.tgz"
  sha256 "f687b307188ea030cf65f49cc56b036dd6344eb6fd9592907453d3f1a083ca6b"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "37d3898287ea56e26d473114758de71add45b7ad9e5f237bcb853b97da5d21be"
  end

  depends_on "node"

  conflicts_with "jsbeautifier", because: "both install `js-beautify` binaries"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    javascript = "if ('this_is'==/an_example/){of_beautifier();}else{var a=b?(c%d):e[f];}"
    assert_equal <<~EOS.chomp, pipe_output(bin/"js-beautify", javascript)
      if ('this_is' == /an_example/) {
          of_beautifier();
      } else {
          var a = b ? (c % d) : e[f];
      }
    EOS
  end
end
