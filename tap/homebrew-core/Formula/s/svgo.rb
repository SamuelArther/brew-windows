class Svgo < Formula
  desc "Nodejs-based tool for optimizing SVG vector graphics files"
  homepage "https://svgo.dev/"
  url "https://github.com/svg/svgo/archive/refs/tags/v4.1.0.tar.gz"
  sha256 "8c9d43624e108eab29e369fdca68ea160e57b37a364907b499b5d6a4436cb59b"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "27c95a1e4fd4b9727afc708bd1d96124109e2316bbc44dc7a9c2e1b89f4fee8d"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    cp test_fixtures("test.svg"), testpath
    system bin/"svgo", "test.svg", "-o", "test.min.svg"
    assert_match(/^<svg /, (testpath/"test.min.svg").read)
  end
end
