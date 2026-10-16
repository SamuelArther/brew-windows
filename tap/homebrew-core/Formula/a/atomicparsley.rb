class Atomicparsley < Formula
  desc "MPEG-4 command-line tool"
  homepage "https://github.com/wez/atomicparsley"
  url "https://github.com/wez/atomicparsley/releases/download/20240608.083822.1ed9031/AtomicParsleyWindows.zip"
  version "20240608.083822.1ed9031"
  sha256 "cc7c5a7dcaba28869f3819e4a57f382dc0ff307b7b013f9bffd028605ae6f52d"
  license "GPL-2.0-or-later"
  version_scheme 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/wez/atomicparsley/archive/refs/tags/20240608.083822.1ed9031.tar.gz"
  end

  def install
    bin.install "AtomicParsley.exe"
  end

  test do
    system bin/"AtomicParsley", "--version"
  end
end
