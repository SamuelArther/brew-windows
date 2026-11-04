class Oxfmt < Formula
  desc "High-performance formatting tool for JavaScript and TypeScript"
  homepage "https://oxc.rs/"
  url "https://registry.npmjs.org/oxfmt/-/oxfmt-0.76.0.tgz"
  sha256 "556c8448d162c48ed1ed22c50df01d2a8665f5096e15c1189cc968963119906b"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2c7155819607fc914c57566fe9937d026706e1219830656cfa5ad7373a4e0e25"
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
