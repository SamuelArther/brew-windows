class Rulesync < Formula
  desc "Unified AI rules management CLI tool"
  homepage "https://github.com/dyoshikawa/rulesync"
  url "https://registry.npmjs.org/rulesync/-/rulesync-43.4.2.tgz"
  sha256 "12074bfffef1318badd06a5f0f92592c338d7ad42e7fdb24020ace494805c4f7"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "7072202b9761772f9aaa2c02ae8929664ba80e595010a26c337907055569ff97"
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
