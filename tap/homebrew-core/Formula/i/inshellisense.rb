class Inshellisense < Formula
  desc "IDE style command-line auto complete"
  homepage "https://github.com/microsoft/inshellisense"
  url "https://registry.npmjs.org/@microsoft/inshellisense/-/inshellisense-0.1.0.tgz"
  sha256 "60bcadcf98084e73e8ec40f566afe524aa7601363d88f72b08bd20c363065d8a"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2647c678a2de6290939edb2fcbc82667b4fc56c1525810b37c3860ce791df945"
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
