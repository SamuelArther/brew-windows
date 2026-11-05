class Jsrepo < Formula
  desc "Build and distribute your code"
  homepage "https://jsrepo.dev/"
  url "https://registry.npmjs.org/jsrepo/-/jsrepo-3.8.3.tgz"
  sha256 "32f4e6afc5bf816359cf700cd1369da2fb23ed74ea442ac4d580a1db11f3c43b"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "29c7f26b0141bea69e5e8fa673b4b56e7d6fa757b24fc85a69782421537740b6"
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
