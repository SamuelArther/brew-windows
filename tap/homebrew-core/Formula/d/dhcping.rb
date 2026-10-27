class Dhcping < Formula
  desc "Perform a dhcp-request to check whether a dhcp-server is running"
  homepage "https://www.mavetju.org/unix/general.php"
  url "https://www.mavetju.org/download/dhcping-1.2.tar.gz"
  mirror "https://deb.debian.org/debian/pool/main/d/dhcping/dhcping_1.2.orig.tar.gz"
  sha256 "32ef86959b0bdce4b33d4b2b216eee7148f7de7037ced81b2116210bc7d3646a"
  license "BSD-2-Clause"

  livecheck do
    url :homepage
    regex(/href=.*?dhcping[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2f63ed497e4c2d062f2668297aabc5926b12332a2f24677be753b3014dbd5ae2"
  end

  allow_network_access! :test

  def install
    args = ["--mandir=#{man}"]
    # Help old config scripts identify arm64 linux
    args << "--build=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm64?

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    output = shell_output("#{bin}/dhcping -c 10.0.0.2 -s 10.0.0.1 -h 34:e6:d7:0f:a9:83", 1)
    assert_match "This program should only be ran by root or be installed as setuid root.", output
  end
end
