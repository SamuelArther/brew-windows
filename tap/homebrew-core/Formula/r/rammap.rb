class Rammap < Formula
  desc "Extensible and performant aligner and read mapper"
  homepage "https://github.com/jwanglab/rammap"
  url "https://github.com/jwanglab/rammap/archive/refs/tags/v1.2.0.tar.gz"
  sha256 "66d96ba52cd4ed6f1365aa3f1b1e736aeccbbcab71b8b590d3b7cc997b1a7fbd"
  license "MIT"
  head "https://github.com/jwanglab/rammap.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8f74cbdbe74b505156bc8e911e97ca6ac8108ef38e9a0417ebd424a4d2410d78"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "rammap")
  end

  test do
    # Deterministic pseudo-random reference; a repetitive one would not seed.
    seed = 12_345
    reference = Array.new(1200) do
      seed = ((seed * 1_103_515_245) + 12_345) % (2**31)
      "ACGT"[(seed >> 16) % 4]
    end.join
    (testpath/"ref.fa").write ">chr1\n#{reference.scan(/.{1,60}/).join("\n")}\n"

    # A read lifted verbatim out of the reference, so it must align exactly.
    read = reference[300, 200]
    (testpath/"reads.fq").write "@read1\n#{read}\n+\n#{"I" * read.length}\n"

    paf = shell_output("#{bin}/rammap -x map-ont ref.fa reads.fq 2>/dev/null")
    fields = paf.lines.first.chomp.split("\t")
    assert_equal "read1", fields[0]
    assert_equal "+", fields[4]
    assert_equal "chr1", fields[5]
    assert_equal "1200", fields[6]
    assert_equal "60", fields[11]
    assert_operator fields[9].to_i, :>=, 190

    sam = shell_output("#{bin}/rammap -x map-ont -a ref.fa reads.fq 2>/dev/null")
    assert_match "@SQ\tSN:chr1\tLN:1200", sam
    assert_match "200M", sam
    assert_match "NM:i:0", sam
  end
end
