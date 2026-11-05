class Stress < Formula
  desc "Tool to impose load on and stress test a computer system"
  homepage "https://github.com/resurrecting-open-source-projects/stress"
  url "https://github.com/resurrecting-open-source-projects/stress/releases/download/1.0.7/stress-1.0.7.tar.gz"
  sha256 "80d966622c4e8d0981f64f56c06a191ef7c9a32c2660b3b957ef7f014dfd78cd"
  license "GPL-2.0-or-later"

  deny_network_access!

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7b26cfcf7f5178a5f598a90735d3804ceb17125d9f0d41fa96b90b2a356cedc8"
  end

  def install
    system "./configure", "--disable-dependency-tracking",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    system bin/"stress", "--cpu", "2", "--io", "1", "--vm", "1", "--vm-bytes", "128M", "--timeout", "1s", "--verbose"
  end
end
