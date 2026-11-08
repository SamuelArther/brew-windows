class Ctx7 < Formula
  desc "Manage AI coding skills and documentation context"
  homepage "https://context7.com"
  url "https://registry.npmjs.org/ctx7/-/ctx7-0.10.0.tgz"
  sha256 "c53ca779c658adeb3548a1b23f68af1a864fc14dca00441a0636f78c7cab9547"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a59e9d4de6773e2e60615024e34e5d995be725e3260beb1b586cb2791a7957af"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ctx7 --version")
    assert_match "Not logged in", shell_output("#{bin}/ctx7 whoami")
    system bin/"ctx7", "library", "react", "hooks"
  end
end
