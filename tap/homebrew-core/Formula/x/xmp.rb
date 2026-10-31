class Xmp < Formula
  desc "Command-line player for module music formats (MOD, S3M, IT, etc)"
  homepage "https://xmp.sourceforge.net/"
  url "https://github.com/libxmp/xmp-cli/releases/download/xmp-4.3.1/xmp-4.3.1.tar.gz"
  sha256 "cbfdab11233708c4de6ab965f64d96d4cb5b9d8e14d2d23df3b1b896386f870f"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "17a437fb792de928d98db89b80d2648645c9e0df7513d0741dc433328267ee40"
  end

  head do
    url "https://github.com/libxmp/xmp-cli.git", branch: "master"

    depends_on "autoconf" => :build
    depends_on "automake" => :build
    depends_on "libtool"  => :build
  end

  depends_on "pkgconf" => :build
  depends_on "libxmp"

  deny_network_access!

  def install
    if build.head?
      system "glibtoolize"
      system "aclocal"
      system "autoconf"
      system "automake", "--add-missing"
    end

    system "./configure", "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    assert_match "Fast Tracker II", shell_output("#{bin}/xmp --list-formats")
    assert_match "Extended Module Player #{version}", shell_output("#{bin}/xmp --version")
  end
end
