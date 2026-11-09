class Codeburn < Formula
  desc "See where your AI coding tokens go - by task, tool, model, and project"
  homepage "https://codeburn.app/"
  url "https://registry.npmjs.org/codeburn/-/codeburn-0.9.28.tgz"
  sha256 "e4b0a41b70331c1e85b6fbbc4c7cb0dc75fb43de17790b51c737a42e034a6e2e"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "03026e10509ba927cb5c91578d90b84f219db7bcfcb7b0fdb14fe69dbe31f1cb"
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
