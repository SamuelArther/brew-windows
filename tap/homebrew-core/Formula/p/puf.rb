class Puf < Formula
  desc "Parallel URL fetcher"
  homepage "https://puf.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/puf/puf/1.0.0/puf-1.0.0.tar.gz"
  sha256 "3f1602057dc47debeb54effc2db9eadcffae266834389bdbf5ab14fc611eeaf0"
  license "GPL-2.0-only"

  allow_network_access! :test

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "429b5a3896dfa518a00f91c1a2870738873e2ebfbc40afe688e9b31915e3f8a0"
  end

  def install
    args = []
    # Help old config scripts identify arm64 linux
    args << "--build=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm64?

    system "./configure", "--mandir=#{man}", *args, *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"puf", "-r", "example.com"
  end
end
