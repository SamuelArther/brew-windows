class Alexjs < Formula
  desc "Catch insensitive, inconsiderate writing"
  homepage "https://alexjs.com"
  url "https://github.com/get-alex/alex/archive/refs/tags/11.0.1.tar.gz"
  sha256 "0c41d5d72c0101996aecb88ae2f423d6ac7a2fc57f93384d1a193d2ce67c4ffb"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "ac0cdae2d58d191e7a06bdb59f31caf3e3cd4c40a572817b05acf1e2ff6ac137"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/"test.txt").write "garbageman"
    assert_match "garbage collector", shell_output("#{bin}/alex test.txt 2>&1", 1)
  end
end
