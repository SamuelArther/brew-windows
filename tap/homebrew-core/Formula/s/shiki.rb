class Shiki < Formula
  desc "Beautiful yet powerful syntax highlighter"
  homepage "https://shiki.style/"
  url "https://registry.npmjs.org/@shikijs/cli/-/cli-4.6.0.tgz"
  sha256 "2e559524fddfb9d0d4802b68ca8276f9573f26c55a9d8b7155f873bd6397c224"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "aec79c2d1fcbfa20e423ad597a48004eecd842e68abb02f83e2efe5237662148"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/shiki --version")

    (testpath/"test.txt").write <<~TXT
      Hello, world!
    TXT

    assert_match "Hello, world!", shell_output("#{bin}/shiki #{testpath}/test.txt")
  end
end
