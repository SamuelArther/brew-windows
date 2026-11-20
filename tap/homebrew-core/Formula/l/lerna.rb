class Lerna < Formula
  desc "Tool for managing JavaScript projects with multiple packages"
  homepage "https://lerna.js.org"
  url "https://registry.npmjs.org/lerna/-/lerna-10.1.2.tgz"
  sha256 "ee91c398736d85ad998d14aee6f3fbd3787b307d7167bb562ce8991655f42f9a"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "1905d32a033955fc27b35afea9adc3407669d1aeb05d89e762aa03ef35ff1b88"
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
