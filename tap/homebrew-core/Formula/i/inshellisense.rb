class Inshellisense < Formula
  desc "IDE style command-line auto complete"
  homepage "https://github.com/microsoft/inshellisense"
  url "https://registry.npmjs.org/@microsoft/inshellisense/-/inshellisense-0.0.4.tgz"
  sha256 "413c9a1657bb5b31353dbb372e25934a712c57857bd09199889f6cb64c441fb7"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6ee2176aa61cb0119f447da5eb59d5ad81af09572e49289be446f62b5a7efe10"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match "inshellisense session", shell_output("#{bin}/is --check")
  end
end
