class Lr < Formula
  desc "File list utility with features from ls(1), find(1), stat(1), and du(1)"
  homepage "https://github.com/leahneukirchen/lr"
  url "https://github.com/leahneukirchen/lr/archive/refs/tags/v2.0.1.tar.gz"
  sha256 "c7c4a57169e1396f17a09b05b19456945c8bb5e55001c5a870b083c0b4a23cd8"
  license "MIT"
  head "https://github.com/leahneukirchen/lr.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f4e3a04c1ee802ffed1b74537e4100218efde8da4838e17427dbeb7f453b67c1"
  end

  def install
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    assert_match(/^\.\n(.*\n)?\.bazelrc\n/, shell_output("#{bin}/lr -1"))
  end
end
