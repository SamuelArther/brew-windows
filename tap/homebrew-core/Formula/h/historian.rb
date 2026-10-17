class Historian < Formula
  desc "Command-line utility for managing shell history in a SQLite database"
  homepage "https://github.com/jcsalterego/historian"
  url "https://github.com/jcsalterego/historian/archive/refs/tags/0.0.2.tar.gz"
  sha256 "691b131290ddf06142a747755412115fec996cb9cc2ad8e8f728118788b3fe05"
  license "BSD-2-Clause"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/historian-0.0.2"
    sha256 cellar: :any, x86_64_windows: "05facebd339bf62d8eafcc98fd0725c2fe4f579ff76550546ef17bc413abddd8"
  end

  uses_from_macos "sqlite"

  def install
    bin.install "hist"
  end

  test do
    ENV["HISTORIAN_SRC"] = "test_history"
    (testpath/"test_history").write <<~EOS
      brew update
      brew upgrade
    EOS
    system bin/"hist", "import"
  end
end
