class Miniprot < Formula
  desc "Align proteins to genomes with splicing and frameshift"
  homepage "https://lh3.github.io/miniprot/"
  url "https://github.com/lh3/miniprot/archive/refs/tags/v0.18.tar.gz"
  sha256 "e1b5c08571fa3a4aa225da8ec9c6e744cd116b4dc50d9e187114cffe336921ee"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/miniprot-0.18"
    sha256 cellar: :any, x86_64_windows: "eb9a5812b41682e173fcbd23fc1e11be69062737c9122a8b5770297390facc21"
  end

  on_linux do
    depends_on "zlib-ng-compat"
  end

  def install
    system "make"
    bin.install "miniprot"
    pkgshare.install "test"
  end

  test do
    cp_r pkgshare/"test/.", testpath
    output = shell_output("#{bin}/miniprot DPP3-hs.gen.fa.gz DPP3-mm.pep.fa.gz 2>&1")
    assert_match "mapped 1 sequences", output
  end
end
