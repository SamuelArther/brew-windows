class Fanyi < Formula
  desc "Chinese and English translate tool in your command-line"
  homepage "https://github.com/afc163/fanyi"
  url "https://registry.npmjs.org/fanyi/-/fanyi-11.0.0.tgz"
  sha256 "7893ae87298c3731ea4a846e0c766db698c709d9b13d20f16bb8177e8d1c7100"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "0dc64dac74fce7adc4ee568f13c780793a7d3a1f088736cecef5d3a7d9fed9b1"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match ".fanyirc", shell_output("#{bin}/fanyi config list ")
    assert_match version.to_s, shell_output("#{bin}/fanyi --version")
  end
end
