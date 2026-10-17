class Coffeescript < Formula
  desc "Unfancy JavaScript"
  homepage "https://coffeescript.org/"
  url "https://registry.npmjs.org/coffeescript/-/coffeescript-2.7.0.tgz"
  sha256 "590e2036bd24d3b54e598b56df2e0737a82c2aa966c1020338508035f3b4721f"
  license "MIT"
  head "https://github.com/jashkenas/coffeescript.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "be505f4c1f78848af5156983352ed9d5e2ea7454dae058d215bfcd7673fe1eda"
  end

  depends_on "node"

  conflicts_with "cake", because: "both install `cake` binaries"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/"test.coffee").write <<~COFFEESCRIPT
      square = (x) -> x * x
      list = [1, 2, 3, 4, 5]

      math =
        root:   Math.sqrt
        square: square
        cube:   (x) -> x * square x

      cubes = (math.cube num for num in list)
    COFFEESCRIPT

    system bin/"coffee", "--compile", "test.coffee"
    assert_path_exists testpath/"test.js", "test.js was not generated"
  end
end
