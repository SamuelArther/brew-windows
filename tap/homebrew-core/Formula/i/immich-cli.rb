class ImmichCli < Formula
  desc "Command-line interface for self-hosted photo manager Immich"
  homepage "https://immich.app/docs/features/command-line-interface"
  url "https://registry.npmjs.org/@immich/cli/-/cli-3.4.2.tgz"
  sha256 "8d969b209e8bae9c24138eed99352ee813f03a3b36528802d60de9f50fa39f7b"
  license "AGPL-3.0-only"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f819e833b007b76012decbd660b534764be2c1e0708437a1bc3dfb4f5f21ae70"
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
