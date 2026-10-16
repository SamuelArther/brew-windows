class Nanoq < Formula
  desc "Minimal but speedy quality control and summaries of nanopore reads"
  homepage "https://github.com/esteinig/nanoq"
  url "https://github.com/esteinig/nanoq/archive/refs/tags/0.10.0.tar.gz"
  sha256 "c6ab28a5c738be950bfbf36ddf9e0e46216a9a1aa040d6745fcb9d87cc32534a"
  license "MIT"
  head "https://github.com/esteinig/nanoq.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b881a7a13b7117399e1b2f970e95cd42bc3458568495aafed7c55dc55ad37680"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    (testpath/"test.fq").write <<~EOS
      @read1
      ACGTACGTACGTACGTACGTACGTACGTACGT
      +
      IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII
      @read2
      ACGT
      +
      IIII
    EOS

    # Filter out reads shorter than 10 bp; only read1 should remain
    output = shell_output("#{bin}/nanoq -i test.fq -l 10")
    assert_includes output, "read1"
    refute_includes output, "read2"

    # Verbose summary report should count the 2 input reads
    report = shell_output("#{bin}/nanoq -i test.fq -s -v")
    assert_match "Number of reads:      2", report
  end
end
