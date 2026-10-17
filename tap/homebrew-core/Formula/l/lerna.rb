class Lerna < Formula
  desc "Tool for managing JavaScript projects with multiple packages"
  homepage "https://lerna.js.org"
  url "https://registry.npmjs.org/lerna/-/lerna-10.1.1.tgz"
  sha256 "ffa7bfd387c52f8e22d367dbcc91da4d873a814e02f42a01381e35279967b4ea"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f87a46d38178f92dec8999feade39bacc6f8834e362595939239520928c7d45f"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lerna --version")

    output = shell_output("#{bin}/lerna init --independent 2>&1")
    assert_match "lerna success Initialized Lerna files", output
  end
end
