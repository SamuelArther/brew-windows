class Nrm < Formula
  desc "NPM registry manager, fast switch between different registries"
  homepage "https://github.com/Pana/nrm"
  url "https://registry.npmjs.org/nrm/-/nrm-2.1.0.tgz"
  sha256 "cdad289ac8e72878ab72575ee61551b5d1cb6334097d6904f5ce30603ae5c74f"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "42fecd17f94c48def05d286e8940260ed67b21bc773994d342bd4d3702b0946f"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match "SUCCESS", shell_output("#{bin}/nrm add test http://localhost")
    assert_match "test --------- http://localhost/", shell_output("#{bin}/nrm ls")
    assert_match "SUCCESS", shell_output("#{bin}/nrm del test")
  end
end
