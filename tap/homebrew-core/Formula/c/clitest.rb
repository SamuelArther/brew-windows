class Clitest < Formula
  desc "Command-Line Tester"
  homepage "https://github.com/aureliojargas/clitest"
  url "https://github.com/aureliojargas/clitest/archive/refs/tags/0.5.0.tar.gz"
  sha256 "4005de0bc27e4676e418ab1e1e64861272aa74af1212c73a1173760fc449b049"
  license "MIT"
  compatibility_version 1
  head "https://github.com/aureliojargas/clitest.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "c233812f48261c71abc16332a162cfc4670ae7a0e797d5b7b21882b4b39c3896"
  end

  def install
    bin.install "clitest"
  end

  test do
    (testpath/"test.txt").write <<~EOS
      $ echo "Hello World"   #=> Hello World
      $ cd /tmp
      $ pwd                  #=> /tmp
      $ cd "$OLDPWD"
      $
    EOS
    assert_match "OK: 4 of 4 tests passed",
      shell_output("#{bin}/clitest #{testpath}/test.txt")
  end
end
