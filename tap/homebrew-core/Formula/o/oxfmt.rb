class Oxfmt < Formula
  desc "High-performance formatting tool for JavaScript and TypeScript"
  homepage "https://oxc.rs/"
  url "https://registry.npmjs.org/oxfmt/-/oxfmt-0.78.0.tgz"
  sha256 "efcd09f280a59f1bbd8cf961c7e438fe8ae34d9c7531e7b55afa2a1b100e944b"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4177db1df682a053efb87669072323629f063c1368b0c55e5119ecc1bf5a1ca9"
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
