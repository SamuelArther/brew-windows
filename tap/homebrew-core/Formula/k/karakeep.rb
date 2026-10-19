class Karakeep < Formula
  desc "CLI tool for self-hostable bookmark-everything app karakeep"
  homepage "https://karakeep.app/"
  url "https://registry.npmjs.org/@karakeep/cli/-/cli-0.34.0.tgz"
  sha256 "0cccbeb191bcda965da89b0ad3e6b025bfe637cda27ee49187ac715306558c6f"
  license "AGPL-3.0-only"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2c2a4c54e743f642dcdb90edec922b5f5b612580eccf84fe9d8bff22f060dee0"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/karakeep --version")

    ENV["KARAKEEP_API_KEY"] = "invalid"
    ENV["KARAKEEP_SERVER_ADDR"] = "localhost:#{free_port}"

    assert_match "Error: Failed to query bookmarks", shell_output("#{bin}/karakeep bookmarks list 2>&1")
  end
end
