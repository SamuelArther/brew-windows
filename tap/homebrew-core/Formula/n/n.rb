class N < Formula
  desc "Node version management"
  homepage "https://github.com/tj/n"
  url "https://github.com/tj/n/archive/refs/tags/v11.0.0.tar.gz"
  sha256 "44e0bfd66aadff042817d1da90adaf5dfd16d269aefb4f0c79b5a4da23bd0abd"
  license "MIT"
  head "https://github.com/tj/n.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4dd5581c7cae217f8dfd3ffdbb474fbc04451899c262bb1352820ffc2b37138d"
  end

  def install
    bin.mkdir
    system "make", "PREFIX=#{prefix}", "install"
  end

  test do
    system bin/"n", "ls"
  end
end
