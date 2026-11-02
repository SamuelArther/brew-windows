class PlaywrightCli < Formula
  desc "CLI for Playwright: record/generate code, inspect selectors, take screenshots"
  homepage "https://playwright.dev"
  url "https://registry.npmjs.org/@playwright/cli/-/cli-0.1.23.tgz"
  sha256 "70a706ca63b053dba5e0103d0d96758aee20ed916fe8b32721f886c99d0ec2f4"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "98cba8b3e0622a22a3470d7374b8ffc2fd73a33749fea5a0ed42033de200a668"
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
