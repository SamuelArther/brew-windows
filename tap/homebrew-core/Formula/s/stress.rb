class Stress < Formula
  desc "Tool to impose load on and stress test a computer system"
  homepage "https://github.com/resurrecting-open-source-projects/stress"
  url "https://github.com/resurrecting-open-source-projects/stress/releases/download/1.0.7/stress-1.0.7.tar.gz"
  sha256 "80d966622c4e8d0981f64f56c06a191ef7c9a32c2660b3b957ef7f014dfd78cd"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "1ebf9b0ded499ce10b0b9bf61a40d9fb2270f36a2d1f0ca9a6f60d80671c3ba2"
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
