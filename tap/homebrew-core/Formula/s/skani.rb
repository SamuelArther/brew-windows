class Skani < Formula
  desc "Fast, robust ANI and aligned fraction for (metagenomic) genomes and contigs"
  homepage "https://github.com/bluenote-1577/skani"
  url "https://github.com/bluenote-1577/skani/archive/refs/tags/v0.4.0.tar.gz"
  sha256 "c8fe23ffae0119aa79cc801ac08dfc33862c94006690074daff274d937f1f786"
  license "MIT"
  head "https://github.com/bluenote-1577/skani.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "570997d0bfa7b7be0d85a6cc4b4f8216cb48e4f26576d8c1fb40cb2795f653cc"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
    pkgshare.install "test_files"
  end

  test do
    cp_r pkgshare/"test_files/.", testpath
    output = shell_output("#{bin}/skani dist e.coli-EC590.fasta e.coli-K12.fasta")
    assert_match "complete sequence", output
  end
end
