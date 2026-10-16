class Sift < Formula
  desc "Fast and powerful open source alternative to grep"
  homepage "https://sift-tool.org/"
  url "https://github.com/svent/sift/releases/download/v0.9.2/sift_0.9.2_Windows_x86_64.zip"
  sha256 "3ddcc65ffb90381019e6bcf0f928ecc116f299aa6a45badfae2f44317f85381c"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/svent/sift/archive/refs/tags/v0.9.2.tar.gz"
  end

  def install
    bin.install "sift.exe"
  end

  test do
    (testpath/"test.txt").write("where is foo\n")
    assert_match "where is foo", shell_output("#{bin}/sift foo #{testpath}")
  end
end
