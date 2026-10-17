class Pdfcrack < Formula
  desc "PDF files password cracker"
  homepage "https://pdfcrack.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/pdfcrack/pdfcrack/pdfcrack-0.21/pdfcrack-0.21.tar.gz"
  sha256 "26f00d4afcb70b5839047bc6f62e4253073ac437bdb526f01e8c04b220e97762"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e3c536e3158a00c42e72587e8e777036ab48a4422dc48954b3f1b60a64512b3c"
  end

  def install
    system "make", "all"
    bin.install "pdfcrack"
  end

  test do
    system bin/"pdfcrack", "--version"
  end
end
