class Oxfmt < Formula
  desc "High-performance formatting tool for JavaScript and TypeScript"
  homepage "https://oxc.rs/"
  url "https://registry.npmjs.org/oxfmt/-/oxfmt-0.79.0.tgz"
  sha256 "6298c21d0461876b41dca405cf96a6006a4d8170d3df3c6f4a3801fef46a424b"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "52cba8c41b95e3ecd9e91c983c707f8115fffbed85ed66470bf8cfed4e49a0df"
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
