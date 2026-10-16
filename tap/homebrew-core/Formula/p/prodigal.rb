class Prodigal < Formula
  desc "Microbial gene prediction"
  homepage "https://github.com/hyattpd/Prodigal"
  url "https://github.com/hyattpd/Prodigal/releases/download/v2.6.3/prodigal.windows.exe"
  sha256 "d2ec9ca562a10959df2812be771e5c64df6c230dbebee52c3ec906b51749888a"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/hyattpd/Prodigal/archive/refs/tags/v2.6.3.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "prodigal.windows.exe" => "prodigal.exe"
  end

  test do
    fasta = <<~EOS
      >U00096.2:1-70
      AGCTTTTCATTCTGACTGCAACGGGCAATATGTCTCTGTGTGGATTAAAAAAAGAGTGTCTGATAGCAGC
    EOS
    assert_match "CDS", pipe_output("#{bin}/prodigal -q -p meta", fasta, 0)
  end
end
