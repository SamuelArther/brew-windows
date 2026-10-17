class Sparse < Formula
  desc "Static C code analysis tool"
  homepage "https://sparse.wiki.kernel.org/"
  url "https://mirrors.edge.kernel.org/pub/software/devel/sparse/dist/sparse-0.6.4.tar.xz"
  sha256 "6ab28b4991bc6aedbd73550291360aa6ab3df41f59206a9bde9690208a6e387c"
  license "MIT"
  head "https://git.kernel.org/pub/scm/devel/sparse/sparse.git", branch: "master"

  livecheck do
    url "https://mirrors.edge.kernel.org/pub/software/devel/sparse/dist/"
    regex(/href=.*?sparse[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/sparse-0.6.4"
    sha256 cellar: :any, x86_64_windows: "d2b389c961960201175c22c2e51ad2232454e6f5de4c5b483e5d1a44eafc25b5"
  end

  def install
    # BSD "install" does not understand the GNU -D flag.
    # Create the parent directories ourselves.
    inreplace "Makefile", "install -D", "install"
    bin.mkpath
    man1.mkpath

    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    (testpath/"test.C").write("int main(int a) {return a;}\n")
    system bin/"sparse", testpath/"test.C"
  end
end
