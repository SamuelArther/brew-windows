class Mongosh < Formula
  desc "MongoDB Shell to connect, configure, query, and work with your MongoDB database"
  homepage "https://www.mongodb.com/try/download/shell"
  url "https://registry.npmjs.org/@mongosh/cli-repl/-/cli-repl-2.14.3.tgz"
  sha256 "8e21711ae4d58e83764a41d0ca59c3374355804343b06a6abd648577aa426ecd"
  license "Apache-2.0"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "0c8203680ef9904f97f4bab5cee4647bb3c9d7e94b0c52f7bb475703255f2ddd"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match "ECONNREFUSED 0.0.0.0:1", shell_output("#{bin}/mongosh \"mongodb://0.0.0.0:1\" 2>&1", 1)
    assert_match "#ok#", shell_output("#{bin}/mongosh --nodb --eval \"print('#ok#')\"")
    assert_match "all tests passed", shell_output("#{bin}/mongosh --smokeTests 2>&1")
  end
end
