class Oxfmt < Formula
  desc "High-performance formatting tool for JavaScript and TypeScript"
  homepage "https://oxc.rs/"
  url "https://registry.npmjs.org/oxfmt/-/oxfmt-0.75.0.tgz"
  sha256 "f95a90171c00ec55feccb008d768c5217a70ceae68495bd781dfd59756d5cec8"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8b90f9c06b4c46971ae9bdc9696ce288d257ee9534608ba437797165e0821d11"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/"test.js").write("const arr = [1,2];")
    system bin/"oxfmt", "test.js"
    assert_equal "const arr = [1, 2];\n", (testpath/"test.js").read
  end
end
