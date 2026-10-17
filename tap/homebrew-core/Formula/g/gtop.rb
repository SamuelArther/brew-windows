class Gtop < Formula
  desc "System monitoring dashboard for terminal"
  homepage "https://github.com/aksakalli/gtop"
  url "https://registry.npmjs.org/gtop/-/gtop-1.1.5.tgz"
  sha256 "a8e90b828e33160c6a0ac4fb11231f292496e8049c0dac814e46fdd0c90817c1"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8e18bae8fc86e21f879bf49375f24ead1495198439e65d5d0dfc0a531fae0ee9"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink Dir[libexec/"bin/*"]
  end

  test do
    assert_match "Error: Width must be multiple of 2", shell_output("#{bin}/gtop 2>&1", 1)
  end
end
