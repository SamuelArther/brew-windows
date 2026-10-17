class ImmichCli < Formula
  desc "Command-line interface for self-hosted photo manager Immich"
  homepage "https://immich.app/docs/features/command-line-interface"
  url "https://registry.npmjs.org/@immich/cli/-/cli-3.3.3.tgz"
  sha256 "552a8f66b792e7a0eb16778afce27582df71b5da146e5e39b3da249a4da2860c"
  license "AGPL-3.0-only"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4976136f4b5ea061da71959336825cfbb4844ffb5004ddb0fd9f770ab1fcc782"
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
