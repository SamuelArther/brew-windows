class RandomizeLines < Formula
  desc "Reads and randomize lines from a file (or STDIN)"
  homepage "https://arthurdejong.org/rl/"
  url "https://arthurdejong.org/rl/rl-0.2.7.tar.gz"
  sha256 "1cfca23d6a14acd190c5a6261923757d20cb94861c9b2066991ec7a7cae33bc8"
  license "GPL-2.0-or-later"

  livecheck do
    url :homepage
    regex(/href=.*?rl[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "266f14300e2098357f329d5714a3471f63d0ac988462e7efc9fe3476f6fe87f6"
  end

  deny_network_access!

  def install
    system "./configure", "--disable-dependency-tracking",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    (testpath/"test.txt").write <<~EOS
      1
      2
      4
    EOS

    system bin/"rl", "-c", "1", testpath/"test.txt"
  end
end
