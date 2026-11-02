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
    sha256 cellar: :any_skip_relocation, x86_64_windows: "40411b97acc4370890d80cc552fd0fb94c6214726fb58dc38bdd3849f0786858"
  end

  depends_on "lzip" => :test

  deny_network_access!

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
