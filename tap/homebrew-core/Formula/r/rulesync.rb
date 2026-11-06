class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.6.1.tgz"
  sha256 "9aa668fa20f4e615eab98f0f52a4970dd80f79ab8f07738a6ab9d0bc7b8e0358"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "96559ebfcd9199a9179e22844cb49ef14ea2aa5f2ffb831781f91b118c35ccdb"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rulesync --version")

    output = shell_output("#{bin}/rulesync init")
    assert_match "rulesync initialized successfully", output
    assert_match "Project overview and general development guidelines", (testpath/".rulesync/rules/overview.md").read
  end
end
