class Hmmer < Formula
  desc "Build profile HMMs and scan against sequence databases"
  homepage "https://hmmer.org/"
  url "https://eddylab.org/software/hmmer/hmmer-3.4.tar.gz"
  mirror "https://distfiles.macports.org/hmmer/hmmer-3.4.tar.gz"
  sha256 "ca70d94fd0cf271bd7063423aabb116d42de533117343a9b27a65c17ff06fbf3"
  license "BSD-3-Clause"

  livecheck do
    url "https://hmmer.org/archive.html"
    regex(/href=.*?hmmer[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "812702d0969a9baaf047adccb269ca374fc92b1b27acfce6f5776bda640c6dc7"
  end

  deny_network_access!

  def install
    system "./configure", "--prefix=#{prefix}"
    system "make", "install"
    doc.install "Userguide.pdf", "tutorial"
  end

  test do
    assert_match "PF00069.17", shell_output("#{bin}/hmmstat #{doc}/tutorial/Pkinase.hmm")
  end
end
