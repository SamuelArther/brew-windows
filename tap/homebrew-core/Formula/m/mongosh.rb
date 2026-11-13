class Mongosh < Formula
  desc "MongoDB Shell to connect, configure, query, and work with your MongoDB database"
  homepage "https://www.mongodb.com/try/download/shell"
  url "https://registry.npmjs.org/@mongosh/cli-repl/-/cli-repl-2.14.2.tgz"
  sha256 "1efaa21ac89c487353da83e3b5a59c2986adaf7da0d984d4a8d9859c70d9daae"
  license "Apache-2.0"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "a8d8058446926655e2ddd919affcd1bf1d8c26b0204ce6a722e6202b3b778c36"
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
