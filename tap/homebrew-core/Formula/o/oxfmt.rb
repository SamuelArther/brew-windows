class Oxfmt < Formula
  desc "High-performance formatting tool for JavaScript and TypeScript"
  homepage "https://oxc.rs/"
  url "https://registry.npmjs.org/oxfmt/-/oxfmt-0.77.0.tgz"
  sha256 "b8cea1a23fa46904ba6aecaad2830896547540d2247806bab5653ed40125a685"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a5850c26af373b9e603ae0aedd63ce3f5398a80ea487fafd78e656ec68481657"
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
