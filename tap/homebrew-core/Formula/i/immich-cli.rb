class ImmichCli < Formula
  desc "Command-line interface for self-hosted photo manager Immich"
  homepage "https://immich.app/docs/features/command-line-interface"
  url "https://registry.npmjs.org/@immich/cli/-/cli-3.3.5.tgz"
  sha256 "107b96404ce519a95e7326505afac98ecdd08bcfd7131e4dc7855d0f6b40c6f5"
  license "AGPL-3.0-only"

  bottle do
    sha256 cellar: :any, x86_64_windows: "5e0b9c9dfe1a2adc90a53424f90cf2e11cb4e5b8e1363d3ac7e7d354eb1b925d"
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
