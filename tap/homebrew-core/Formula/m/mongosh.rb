class Mongosh < Formula
  desc "MongoDB Shell to connect, configure, query, and work with your MongoDB database"
  homepage "https://www.mongodb.com/try/download/shell"
  url "https://registry.npmjs.org/@mongosh/cli-repl/-/cli-repl-2.14.1.tgz"
  sha256 "0e9925bfe8a146704c1dc169223f4f69b3ea5e17c59e7cd1fefc58eb2e3e2158"
  license "Apache-2.0"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "a1927b0b06e4b8da4510ad97f33de59a17d92bc8e5e35cef04d647f0af403ad3"
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
