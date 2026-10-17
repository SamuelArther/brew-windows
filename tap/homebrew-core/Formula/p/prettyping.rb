class Prettyping < Formula
  desc "Wrapper to colorize and simplify ping's output"
  homepage "https://denilsonsa.github.io/prettyping/"
  url "https://github.com/denilsonsa/prettyping/archive/refs/tags/v1.1.0.tar.gz"
  sha256 "e8484492e3c704b2460a00b0e417a07ad7112b5f4ad9a211931ee031fe64b4b6"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/prettyping-1.1.0"
    sha256 cellar: :any, x86_64_windows: "4b0ad00c2bf15e404311f60c412f86688ee0994e6fe5c5efd743305242b12aec"
  end

  def install
    bin.install "prettyping"
  end

  test do
    system bin/"prettyping", "-c", "3", "127.0.0.1"
  end
end
