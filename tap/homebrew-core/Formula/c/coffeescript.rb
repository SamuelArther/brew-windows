class Coffeescript < Formula
  desc "Unfancy JavaScript"
  homepage "https://coffeescript.org/"
  url "https://registry.npmjs.org/coffeescript/-/coffeescript-2.7.1.tgz"
  sha256 "895be18c82b7310075571dfc1a032aaed36ecc1bf9a1803007bd6bd6e0c59b40"
  license "MIT"
  head "https://github.com/jashkenas/coffeescript.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "66b383f0509f7c0d499fc0a802705498a7e9598f059ee8324678102997b29ff5"
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
