class Trimal < Formula
  desc "Automated alignment trimming in large-scale phylogenetic analyses"
  homepage "https://trimal.readthedocs.io/"
  url "https://github.com/inab/trimal/archive/refs/tags/v1.5.1.tar.gz"
  sha256 "58751054861b152e92214ff8c01a132071230614e8e777a7c9280d03648cde3b"
  license "GPL-3.0-only"
  head "https://github.com/inab/trimal.git", branch: "trimAl"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/trimal-1.5.1"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "ce248965391f7a9d7f89e95bdacdd06fdb9f8b4dcf511e1b9b6e8bf5e23aeb51"
  end

  def install
    cd "source" do
      system "make"
      bin.install "readal", "trimal", "statal"
    end
  end

  test do
    (testpath/"test.fasta").write <<~FASTA
      >U00096.2:1-70
      AGCTTTTCATTCTGACTGCAACGGGCAATATGTCTCTGTGTGGATTAAAAAAAGAGTGTCTGATAGCAGC
    FASTA
    system bin/"trimal", "-in", "test.fasta", "-out", "out.fasta"
    assert_path_exists "out.fasta"
  end
end
