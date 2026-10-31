class Yo < Formula
  desc "CLI tool for running Yeoman generators"
  homepage "https://yeoman.io"
  url "https://registry.npmjs.org/yo/-/yo-7.0.1.tgz"
  sha256 "466f653547a99ae4cf0de84beac13b8a882804f56718d81df3a2327343bbf7f4"
  license "BSD-2-Clause"
  head "https://github.com/yeoman/yo.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "9d3c8d60a3d48ad397372f175ef88d81f65d8aa7e12a2b65014f22dcf0051047"
  end

  depends_on "node"

  allow_network_access! :build

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/yo --version")
    assert_match "Couldn't find any generators", shell_output("#{bin}/yo --generators")
    assert_match "Running sanity checks on your system", shell_output("#{bin}/yo doctor")
  end
end
