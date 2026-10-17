class Iat < Formula
  desc "Converts many CD-ROM image formats to ISO9660"
  homepage "https://sourceforge.net/projects/iat.berlios/"
  url "https://downloads.sourceforge.net/project/iat.berlios/iat-0.1.7.tar.bz2"
  sha256 "fb72c42f4be18107ec1bff8448bd6fac2a3926a574d4950a4d5120f0012d62ca"
  license "GPL-2.0-or-later"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/iat-0.1.7"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "359c21d5f46f3fd7cd610636fd2dbd89ae2b772b66f06c4c6efd6dbcf3c430a5"
  end

  def install
    system "./configure", "--disable-dependency-tracking", "--prefix=#{prefix}",
                          "--includedir=#{include}/iat"
    system "make", "install"
  end

  test do
    system bin/"iat", "--version"
  end
end
