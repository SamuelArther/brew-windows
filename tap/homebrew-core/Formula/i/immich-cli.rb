class ImmichCli < Formula
  desc "Command-line interface for self-hosted photo manager Immich"
  homepage "https://immich.app/docs/features/command-line-interface"
  url "https://registry.npmjs.org/@immich/cli/-/cli-3.4.1.tgz"
  sha256 "5a57ab478936590067bb1c0f76f885624503c999e0a01e1572f784aea76b067d"
  license "AGPL-3.0-only"

  bottle do
    sha256 cellar: :any, x86_64_windows: "9a0bcd269f411455a468a10752bcf8cee4fb314505189b2ed9c7ad3b5353ccdb"
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
