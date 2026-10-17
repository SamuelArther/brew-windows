class Selecta < Formula
  desc "Fuzzy text selector for files and anything else you need to select"
  homepage "https://github.com/garybernhardt/selecta"
  url "https://github.com/garybernhardt/selecta/archive/refs/tags/v0.0.8.tar.gz"
  sha256 "737aae1677fdec1781408252acbb87eb615ad3de6ad623d76c5853e54df65347"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8f13bb26e0acc6094b917ec10cd9e6f9784d25435a34c1d8e2d21eac3a568771"
  end

  uses_from_macos "ruby"

  def install
    bin.install "selecta"
  end

  test do
    system bin/"selecta", "--version"
  end
end
