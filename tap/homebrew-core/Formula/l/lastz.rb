class Lastz < Formula
  desc "Pairwise aligner for DNA sequences"
  homepage "https://lastz.github.io/lastz/"
  url "https://github.com/lastz/lastz/archive/refs/tags/1.04.60.tar.gz"
  sha256 "e66bb419a6599861b1d48c3b209d3746e8008c3ddde33f0dfeaa76e634bccebf"
  license "MIT"
  head "https://github.com/lastz/lastz.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/lastz-1.04.60"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f4a302bbe9a413b33cb4e9fd4d8c7c08fcca4089dd13db1a0967d71ee474b9a7"
  end

  def install
    system "make", "install", "definedForAll=-Wall", "LASTZ_INSTALL=#{bin}"
    doc.install "README.lastz.html"
    pkgshare.install "test_data", "tools"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lastz --version", 1)
    assert_match "MAF", shell_output("#{bin}/lastz --help=formats", 1)
    dir = pkgshare/"test_data"
    assert_match "#:lav", shell_output("#{bin}/lastz #{dir}/pseudocat.fa #{dir}/pseudopig.fa")
  end
end
