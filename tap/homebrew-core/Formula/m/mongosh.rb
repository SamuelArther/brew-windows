class Mongosh < Formula
  desc "MongoDB Shell to connect, configure, query, and work with your MongoDB database"
  homepage "https://www.mongodb.com/try/download/shell"
  url "https://registry.npmjs.org/@mongosh/cli-repl/-/cli-repl-2.13.0.tgz"
  sha256 "a83ce9dbeee09f4dd05fece99e9cef99e674fb7663fdf8b94038510dbba52cfe"
  license "Apache-2.0"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "237877268d91c5eb5f55ad27c00bcb32032f74589ddaea0e77bb1067c3dbbc49"
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
