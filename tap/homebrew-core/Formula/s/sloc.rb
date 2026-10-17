class Sloc < Formula
  desc "Simple tool to count source lines of code"
  homepage "https://github.com/flosse/sloc"
  url "https://registry.npmjs.org/sloc/-/sloc-0.3.2.tgz"
  sha256 "25ac2a41e015a8ee0d0a890221d064ad0288be8ef742c4ec903c84a07e62b347"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "814fc0a9e7cea4b5de1ad799f98fa83511a064bac2bd97cd41d10baa0a6c5bb8"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdio.h>
      int main(void) {
        return 0;
      }
    C

    std_output = <<~CSV
      Path,Physical,Source,Comment,Single-line comment,Block comment,Mixed,Empty block comment,Empty,To Do
      Total,4,4,0,0,0,0,0,0,0
    CSV

    assert_match std_output, shell_output("#{bin}/sloc --format=csv .")
  end
end
