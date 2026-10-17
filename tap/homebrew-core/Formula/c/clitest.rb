class Clitest < Formula
  desc "Command-Line Tester"
  homepage "https://github.com/aureliojargas/clitest"
  url "https://github.com/aureliojargas/clitest/archive/refs/tags/0.5.0.tar.gz"
  sha256 "4005de0bc27e4676e418ab1e1e64861272aa74af1212c73a1173760fc449b049"
  license "MIT"
  head "https://github.com/aureliojargas/clitest.git", branch: "main"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/clitest-0.5.0"
    sha256 cellar: :any, x86_64_windows: "483c105182b3e0835356379ddefda2d3855a96ca00863a3f9d272d7de06655ec"
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
