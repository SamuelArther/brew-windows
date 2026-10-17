class PlaywrightCli < Formula
  desc "CLI for Playwright: record/generate code, inspect selectors, take screenshots"
  homepage "https://playwright.dev"
  url "https://registry.npmjs.org/@playwright/cli/-/cli-0.1.22.tgz"
  sha256 "bb4840be17006e2b7ba856224dc3062f6571d5f647352476f95c5e77ff5d679a"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "38cc87e610475ae1491806872b871063b8fbb22fcd93148236067241b21d6977"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/playwright-cli --version")
    assert_match "no browsers", shell_output("#{bin}/playwright-cli list")
  end
end
