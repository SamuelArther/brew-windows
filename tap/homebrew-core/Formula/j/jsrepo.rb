class Jsrepo < Formula
  desc "Build and distribute your code"
  homepage "https://jsrepo.dev/"
  url "https://registry.npmjs.org/jsrepo/-/jsrepo-3.8.5.tgz"
  sha256 "584644b661dcd7a47440e3d0c010e0c3eb86173a2a1b862e2af1df9a91ab5b52"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "0a25e8280546060f286657789f871294d98c197b5d41ebfa77fef73db585ed59"
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
