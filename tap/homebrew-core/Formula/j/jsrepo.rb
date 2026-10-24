class Jsrepo < Formula
  desc "Build and distribute your code"
  homepage "https://jsrepo.dev/"
  url "https://registry.npmjs.org/jsrepo/-/jsrepo-3.8.2.tgz"
  sha256 "f40403fc216b3ee77161e5e0487a6fb7f54c1ffc93d6e0cf22e4b206f592fc64"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "346403a6df921ee0b5fbc1a06bae003610adb3e66a61567f32f52b52b4800bdd"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jsrepo --version")

    (testpath/"package.json").write <<~JSON
      {
        "name": "test-package",
        "version": "1.0.0"
      }
    JSON
    system bin/"jsrepo", "init", "--yes"
  end
end
