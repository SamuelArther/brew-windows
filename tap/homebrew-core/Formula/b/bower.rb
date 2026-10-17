class Bower < Formula
  desc "Package manager for the web"
  homepage "https://bower.io/"
  url "https://registry.npmjs.org/bower/-/bower-1.8.14.tgz"
  sha256 "00df3dcc6e8b3a4dd7668934a20e60e6fc0c4269790192179388c928553a3f7e"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "bf4851083203d8cbb263c4f94fd3d4de8851a68c04fc18da461d61db5b6720b3"
  end

  depends_on "node"

  conflicts_with "bower-mail", because: "both install `bower` binaries"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    system bin/"bower", "install", "jquery"
    assert_path_exists testpath/"bower_components/jquery/dist/jquery.min.js", "jquery.min.js was not installed"
  end
end
