class Sersniff < Formula
  desc "Program to tunnel/sniff between 2 serial ports"
  homepage "https://www.earth.li/projectpurple/progs/sersniff.html"
  url "https://www.earth.li/projectpurple/files/sersniff-0.0.5.tar.gz"
  sha256 "8aa93f3b81030bcc6ff3935a48c1fd58baab8f964b1d5e24f0aaecbd78347209"
  license "GPL-2.0-or-later"

  livecheck do
    url :homepage
    regex(/href=.*?sersniff[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0baeb4b140a8ad4c6e124f1b418a749dd5adf8fead4f3c5c901bb8eb1af7660f"
  end

  def install
    system "make"
    bin.install "sersniff"
    man8.install "sersniff.8"
  end

  test do
    assert_match(/sersniff v#{version}/,
                 shell_output("#{bin}/sersniff -h 2>&1", 1))
  end
end
