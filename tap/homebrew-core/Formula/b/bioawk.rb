class Bioawk < Formula
  desc "AWK modified for biological data"
  homepage "https://github.com/lh3/bioawk"
  url "https://github.com/lh3/bioawk/archive/refs/tags/v1.0.tar.gz"
  sha256 "5cbef3f39b085daba45510ff450afcf943cfdfdd483a546c8a509d3075ff51b5"
  license "HPND"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6ba895a3c977a5d1205546a797ec2fa5656be4be184ea56c5a485dbf2341480d"
  end

  uses_from_macos "bison" => :build

  on_linux do
    depends_on "zlib-ng-compat"
  end

  def install
    # Fix make: *** No rule to make target `ytab.h', needed by `b.o'.
    ENV.deparallelize

    system "make"
    bin.install "bioawk"
    man1.install "awk.1" => "bioawk.1"
  end

  test do
    (testpath/"test.fasta").write <<~EOS
      >U00096.2:1-70
      AGCTTTTCATTCTGACTGCAACGGGCAATATGTCT
      CTGTGTGGATTAAAAAAAGAGTGTCTGATAGCAGC
    EOS
    cmd = "#{bin}/bioawk -cfastx '{print length($seq)}' test.fasta"
    assert_equal "70", shell_output(cmd).chomp
  end
end
