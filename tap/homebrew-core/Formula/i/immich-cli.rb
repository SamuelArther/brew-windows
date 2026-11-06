class ImmichCli < Formula
  desc "Command-line interface for self-hosted photo manager Immich"
  homepage "https://immich.app/docs/features/command-line-interface"
  url "https://registry.npmjs.org/@immich/cli/-/cli-3.3.6.tgz"
  sha256 "02b2134c89c0e2e7a1d9ddaef845e6c1a8f23a9f3fed6b3c8d756d1892e42e96"
  license "AGPL-3.0-only"

  bottle do
    sha256 cellar: :any, x86_64_windows: "c7c32519246b3ac5545f192d9190cc1d9f54cb781967618eb231e05f7e0b4ac9"
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
