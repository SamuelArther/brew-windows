class YazeAg < Formula
  desc "Yet Another Z80 Emulator (by AG)"
  homepage "https://agl.yaze-ag.de/"
  url "https://agl.yaze-ag.de/devel/yaze-ag-2.51.3.tar.gz"
  sha256 "2b0a90c3bf3a27574b0427cf4579dc2347b371bec3fea5739e1527edf74b2809"
  license "GPL-2.0-or-later"

  livecheck do
    url :homepage
    regex(/href=.*?yaze-ag[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "437a1ece8d9b3abcce02c7d607963aadf27ce04d5018960d8d51d9b4d17d7133"
  end

  conflicts_with "cpm", because: "both install `cpm` binaries"

  deny_network_access!

  def install
    if OS.mac?
      inreplace "Makefile_solaris_gcc-x86_64", "md5sum -b", "md5"
      inreplace "Makefile_solaris_gcc-x86_64", /(LIBS\s+=\s+-lrt)/, '#\1'
    end

    bin.mkpath
    system "make", "-f", "Makefile_solaris_gcc-x86_64",
                   "BINDIR=#{bin}",
                   "MANDIR=#{man1}",
                   "LIBDIR=#{lib}/yaze",
                   "install"
  end

  test do
    (testpath/"cpm").mkpath
    assert_match "yazerc", shell_output("#{bin}/yaze -v", 1)
  end
end
