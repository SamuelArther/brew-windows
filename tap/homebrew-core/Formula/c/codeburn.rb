class Codeburn < Formula
  desc "See where your AI coding tokens go - by task, tool, model, and project"
  homepage "https://codeburn.app/"
  url "https://registry.npmjs.org/codeburn/-/codeburn-0.9.27.tgz"
  sha256 "03e90a178821865af474a5511a985de1c53e63799ffafa34e76f0d8dd58911f0"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6cede7df6034f6e60a20584e44a858e1f1c36752c84571a76ad63847c77ceb44"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink Dir["#{libexec}/bin/*"]
  end

  test do
    output = shell_output("#{bin}/codeburn report --period today --format json")
    assert_match "\"generated\"", output
    assert_match "\"period\":", output
    assert_match "\"overview\"", output
  end
end
