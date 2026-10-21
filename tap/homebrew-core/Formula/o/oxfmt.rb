class Oxfmt < Formula
  desc "High-performance formatting tool for JavaScript and TypeScript"
  homepage "https://oxc.rs/"
  url "https://registry.npmjs.org/oxfmt/-/oxfmt-0.74.0.tgz"
  sha256 "de5196adea5c3f92701c1360add55b3fb97d22a1deecd2b75f3e86d25ffdfe88"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b76be2195ca96c96f6af05094c2cda2d89685259d234318d633fce851680266b"
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
