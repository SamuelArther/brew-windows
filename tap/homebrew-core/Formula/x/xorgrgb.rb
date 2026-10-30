class Xorgrgb < Formula
  desc "X.Org: color names database"
  homepage "https://www.x.org/"
  url "https://xorg.freedesktop.org/archive/individual/app/rgb-1.1.1.tar.gz"
  sha256 "9bceca0821a46ff54989ab08ab00d6dd87eb2779fc15165d848eaa0ddd742fbe"
  license "MIT"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "8ee8400bab51bb83adfd799bc5abd5e55f488ee4b3af6516109bd95babcc6abf"
  end

  depends_on "pkgconf" => :build
  depends_on "util-macros" => :build
  depends_on "xorgproto" => :build

  def install
    args = %W[
      --sysconfdir=#{etc}
      --localstatedir=#{var}
      --disable-silent-rules
    ]

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    assert_match "gray100", shell_output("#{bin}/showrgb").chomp
  end
end
