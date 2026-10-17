class Czg < Formula
  desc "Interactive Commitizen CLI that generate standardized commit messages"
  homepage "https://cz-git.qbb.sh"
  url "https://registry.npmjs.org/czg/-/czg-1.14.0.tgz"
  sha256 "74a4c978a8fc8a1a1b11b4947a568a8693dde45cfb9d23bfde5a04c99a944a5a"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "efc1ac30c345b59f6ad18e389cd460186e6ddd69ac7755e74a8f988b933c4726"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_equal "#{version}\n", shell_output("#{bin}/czg --version")
    # test: git staging verifies is working
    system "git", "init"
    assert_match ">>> No files added to staging! Did you forget to run `git add` ?",
      shell_output("NO_COLOR=1 #{bin}/czg 2>&1", 1)
  end
end
