class Jsonlint < Formula
  desc "JSON parser and validator with a CLI"
  homepage "https://github.com/zaach/jsonlint"
  url "https://registry.npmjs.org/jsonlint/-/jsonlint-1.6.3.tgz"
  sha256 "987f42f0754b7bc0c84967b81fc2b4db0ed2ebe2117ccc5a5faa59e462447723"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b8cf88f5d4461481ec83ad770d0dd3eeca8985f09e39dbbb1ef46e5e728cc0ac"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/"test.json").write('{"name": "test"}')
    system bin/"jsonlint", "test.json"
  end
end
