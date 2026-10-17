class Minimap2 < Formula
  desc "Versatile pairwise aligner for genomic and spliced nucleotide sequences"
  homepage "https://lh3.github.io/minimap2"
  url "https://github.com/lh3/minimap2/archive/refs/tags/v2.31.tar.gz"
  sha256 "bff334a0e4512644e2f3e29944aeec408f49450f4f74dc39fe89e5273869255b"
  license "MIT"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/minimap2-2.31"
    sha256 cellar: :any, x86_64_windows: "f1be41be8d135b0fc3580b9bb5e6072993cdb3919a6958dbd409f5cfe27a8f55"
  end

  on_linux do
    depends_on "zlib-ng-compat"
  end

  def install
    if Hardware::CPU.arm?
      system "make", "arm_neon=1", "aarch64=1", "extra"
    else
      system "make", "extra"
    end
    bin.install "minimap2", "sdust"
    pkgshare.install "test"
  end

  test do
    cp_r pkgshare/"test/.", testpath
    output = shell_output("#{bin}/minimap2 -a MT-human.fa MT-orang.fa 2>&1")
    assert_match "mapped 1 sequences", output
  end
end
