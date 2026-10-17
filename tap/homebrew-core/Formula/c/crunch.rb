class Crunch < Formula
  desc "Wordlist generator"
  homepage "https://sourceforge.net/projects/crunch-wordlist/"
  url "https://downloads.sourceforge.net/project/crunch-wordlist/crunch-wordlist/crunch-3.6.tgz"
  sha256 "6a8f6c3c7410cc1930e6854d1dadc6691bfef138760509b33722ff2de133fe55"
  license "GPL-2.0-only"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "dce401fd1651d08708cd825e40af13bf3a377a2c82934438b33d4d08b2d1707d"
  end

  def install
    system "make", "CC=#{ENV.cc}", "LFS=-D_LARGEFILE_SOURCE -D_FILE_OFFSET_BITS=64"

    bin.install "crunch"
    man1.install "crunch.1"
    share.install Dir["*.lst"]
  end

  test do
    system bin/"crunch", "-v"
  end
end
