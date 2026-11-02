class Utftex < Formula
  desc "Pretty print math in monospace fonts, using a TeX-like syntax"
  homepage "https://github.com/bartp5/libtexprintf"
  url "https://github.com/bartp5/libtexprintf/releases/download/v1.31/libtexprintf-1.31.tar.gz"
  sha256 "8ca5a11d387631a34979295f73f9a2eccc87e17c690dcd4c1454eb65a9e372bc"
  license "GPL-3.0-only"

  deny_network_access!

  bottle do
    sha256 cellar: :any, x86_64_windows: "d44f30b67d12f14a7f9a0cbd5021269eb4504ea3136e528a79cdd878ba69b0de"
  end

  def install
    system "./configure", "--disable-silent-rules", *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"utftex", "\\left(\\frac{hello}{world}\\right)"
  end
end
