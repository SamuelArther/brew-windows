class FontUtil < Formula
  desc "X.Org: Font package creation/installation utilities"
  homepage "https://www.x.org/"
  url "https://xorg.freedesktop.org/archive/individual/font/font-util-1.4.2.tar.xz"
  sha256 "02e4f8afdcf03cc8372ca9c37aa104b1e36b47722dbc79531be08f0a4c622999"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b055d23d9f27d89174eedfe8e9a49c6713bfdccc515c0ab58fbc370d5c703818"
  end

  depends_on "pkgconf" => [:build, :test]
  depends_on "util-macros" => :build

  deny_network_access!

  def install
    args = %W[
      --sysconfdir=#{etc}
      --localstatedir=#{var}
      --with-fontrootdir=#{HOMEBREW_PREFIX}/share/fonts/X11
    ]

    system "./configure", *args, *std_configure_args
    system "make"
    system "make", "fontrootdir=#{share}/fonts/X11", "install"

    dirs = %w[encodings 75dpi 100dpi misc]
    dirs.each do |d|
      (share/"fonts/X11/#{d}").mkpath
      touch share/"fonts/X11/#{d}/.keepme"
    end
  end

  test do
    system "pkgconf", "--exists", "fontutil"
    assert_equal 0, $CHILD_STATUS.exitstatus
  end
end
