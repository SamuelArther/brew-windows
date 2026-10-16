class Seqkit < Formula
  desc "Cross-platform and ultrafast toolkit for FASTA/Q file manipulation in Golang"
  homepage "https://bioinf.shenwei.me/seqkit"
  url "https://github.com/shenwei356/seqkit/releases/download/v2.14.0/seqkit_windows_amd64.exe.tar.gz"
  sha256 "f4501cff43292059d8a49b93289a365cd433626cac6d5a704541f080ad56773f"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/shenwei356/seqkit/archive/refs/tags/v2.14.0.tar.gz"
  end

  def install
    bin.install "seqkit.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/seqkit version 2>&1")
  end
end
