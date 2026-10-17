class Lunzip < Formula
  desc "Decompressor for lzip files"
  homepage "https://www.nongnu.org/lzip/lunzip.html"
  url "https://download.savannah.nongnu.org/releases/lzip/lunzip/lunzip-1.16.tar.gz"
  mirror "https://download-mirror.savannah.gnu.org/releases/lzip/lunzip/lunzip-1.16.tar.gz"
  sha256 "f13809a1aeaf953f32b07f822c3804bfb11056c08d465b93750b4e45190becda"
  license "GPL-2.0-or-later"

  livecheck do
    url "https://download.savannah.gnu.org/releases/lzip/lunzip/"
    regex(/href=.*?lunzip[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/lunzip-1.16"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "46d1d9b9a919dbe8845d402d6ce08bd93b6e8c2f7bbb19d6152f008ee42f097d"
  end

  depends_on "lzip" => :test

  def install
    system "./configure", "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    path = testpath/"data.txt"
    original_contents = "." * 1000
    path.write original_contents

    # compress: data.txt -> data.txt.lz
    system formula_opt_bin("lzip")/"lzip", path
    refute_path_exists path

    # decompress: data.txt.lz -> data.txt
    system bin/"lunzip", "#{path}.lz"
    assert_equal original_contents, path.read
  end
end
