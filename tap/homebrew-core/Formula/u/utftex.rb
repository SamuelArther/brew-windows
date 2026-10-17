class Utftex < Formula
  desc "Pretty print math in monospace fonts, using a TeX-like syntax"
  homepage "https://github.com/bartp5/libtexprintf"
  url "https://github.com/bartp5/libtexprintf/releases/download/v1.31/libtexprintf-1.31.tar.gz"
  sha256 "8ca5a11d387631a34979295f73f9a2eccc87e17c690dcd4c1454eb65a9e372bc"
  license "GPL-3.0-only"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/utftex-1.31"
    sha256 cellar: :any, x86_64_windows: "c6c33a8b115b1f8b32da64bb1d89e797cff0e617a80e0aa5e28f0f9dfd22a397"
  end

  def install
    system "./configure", "--disable-silent-rules", *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"utftex", "\\left(\\frac{hello}{world}\\right)"
  end
end
