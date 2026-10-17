class RobloxTs < Formula
  desc "TypeScript-to-Luau Compiler for Roblox"
  homepage "https://roblox-ts.com/"
  url "https://registry.npmjs.org/roblox-ts/-/roblox-ts-3.0.0.tgz"
  sha256 "94e7ee8db40d0fab605601312838ca8d6d72242e78a8f01e02467f53c4232757"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a6756a87d66a4fdedf6464f86777946d2301e9b8d505a416af696c25d62824ee"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    output = shell_output("#{bin}/rbxtsc 2>&1", 1)
    assert_match "Unable to find tsconfig.json", output

    assert_match version.to_s, shell_output("#{bin}/rbxtsc --version")
  end
end
