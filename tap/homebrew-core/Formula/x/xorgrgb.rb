class Xorgrgb < Formula
  desc "X.Org: color names database"
  homepage "https://www.x.org/"
  url "https://xorg.freedesktop.org/archive/individual/app/rgb-1.1.1.tar.gz"
  sha256 "9bceca0821a46ff54989ab08ab00d6dd87eb2779fc15165d848eaa0ddd742fbe"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "1f4104bd716f06dce560bdb4a564f528d7c4a42e23faaa95b83c088c006bb3af"
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
