class Par < Formula
  desc "Paragraph reflow for email"
  homepage "http://www.nicemice.net/par/"
  url "https://bitbucket.org/amc-nicemice/par/get/1.53.0.tar.bz2"
  sha256 "6109b1811630e1e0e76fc87bf60ad9140440a145c9b4c9412fb36b4f73726a04"
  # par.doc includes a custom license and alternatively allows usage under MIT license
  license any_of: [:cannot_represent, "MIT"]

  livecheck do
    url :homepage
    regex(/href=.*?Par[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c123685fb51ef852bc2abb2207f2a0b62db5effed749f9bc80d16f510a2ec87a"
  end

  conflicts_with "rancid", because: "both install `par` binaries"

  def install
    system "make", "-f", "protoMakefile"
    bin.install "par"
    man1.install Utils::Gzip.compress("par.1")
  end

  test do
    expected = "homebrew\nhomebrew\n"
    assert_equal expected, pipe_output("#{bin}/par 10gqr", "homebrew homebrew", 0)
  end
end
