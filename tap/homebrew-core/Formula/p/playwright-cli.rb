class PlaywrightCli < Formula
  desc "CLI for Playwright: record/generate code, inspect selectors, take screenshots"
  homepage "https://playwright.dev"
  url "https://registry.npmjs.org/@playwright/cli/-/cli-0.1.25.tgz"
  sha256 "4559e4674cb5d10a03e0c15e9f5c892dcfa4f5d8703c49146594b4af322480f4"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3433d42dc5dcd77b33177f21a5e6cf991cc176e3b86a4aba1d5a98ab9bd1e4aa"
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
