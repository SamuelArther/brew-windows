class Oxfmt < Formula
  desc "High-performance formatting tool for JavaScript and TypeScript"
  homepage "https://oxc.rs/"
  url "https://registry.npmjs.org/oxfmt/-/oxfmt-0.73.0.tgz"
  sha256 "2f948dcf03bc630138124e46b8135e0e22c4bfd4f15f99ba98a87c8ca01312b6"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "ab858eeb7b5ea4c00e13d7dc239052ee28bf80b567482b7f66f88ec295450bf6"
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
