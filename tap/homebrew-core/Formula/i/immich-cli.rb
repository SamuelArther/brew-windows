class ImmichCli < Formula
  desc "Command-line interface for self-hosted photo manager Immich"
  homepage "https://immich.app/docs/features/command-line-interface"
  url "https://registry.npmjs.org/@immich/cli/-/cli-3.4.0.tgz"
  sha256 "b5893edd53c316aab07c236f0ee116c92c530b14c1a0457db303a377b913728e"
  license "AGPL-3.0-only"

  bottle do
    sha256 cellar: :any, x86_64_windows: "af57a67cb6cba8faaf22714c91fdc9a8327c680db16bad7775c5a7c3cd81c1e6"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/immich --version")
    assert_match "No auth file exists. Please login first.", shell_output("#{bin}/immich server-info", 1)
  end
end
