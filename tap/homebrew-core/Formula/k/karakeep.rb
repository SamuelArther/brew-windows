class Karakeep < Formula
  desc "CLI tool for self-hostable bookmark-everything app karakeep"
  homepage "https://karakeep.app/"
  url "https://registry.npmjs.org/@karakeep/cli/-/cli-0.35.0.tgz"
  sha256 "ed03903db20b24f6a26c01b91956dc264a4a5d1b34fd6d8fe5ba1d2a1fd5402b"
  license "AGPL-3.0-only"

  bottle do
    sha256 cellar: :any, x86_64_windows: "c1eda50924fc0d3e1bcb3ffb4106360ccab5953ff814391e404a38438f86a2f1"
  end

  depends_on "node"

  allow_network_access! :test

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
