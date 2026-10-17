class Duff < Formula
  desc "Quickly find duplicates in a set of files from the command-line"
  homepage "https://github.com/elmindreda/duff"
  url "https://downloads.sourceforge.net/project/duff/duff/0.5.2/duff-0.5.2.tar.gz"
  sha256 "15b721f7e0ea43eba3fd6afb41dbd1be63c678952bf3d80350130a0e710c542e"
  license "Zlib"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/duff-0.5.2"
    sha256 cellar: :any, x86_64_windows: "71b22acc7620dc0e584d45e1674cb2c350e32c616fafbe6c60a9ee8587e5e18a"
  end

  def install
    args = ["--mandir=#{man}"]
    # Help old config scripts identify arm64 linux
    args << "--build=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm? && Hardware::CPU.is_64_bit?

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    expected = <<~EOS
      2 files in cluster 1 (6 bytes, digest 8843d7f92416211de9ebb963ff4ce28125932878)
      cmp1
      cmp2
    EOS

    (testpath/"cmp1").write "foobar"
    (testpath/"cmp2").write "foobar"

    assert_equal expected, shell_output("#{bin}/duff cmp1 cmp2")
  end
end
