class UtilMacros < Formula
  desc "X.Org: Set of autoconf macros used to build other xorg packages"
  homepage "https://www.x.org/"
  url "https://xorg.freedesktop.org/archive/individual/util/util-macros-1.20.2.tar.xz"
  sha256 "9ac269eba24f672d7d7b3574e4be5f333d13f04a7712303b1821b2a51ac82e8e"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a7593542431cb12925c98b72e24fc27247aaf4e334ac3c85e6821c3bd817e99d"
  end

  depends_on "pkgconf" => :test

  def install
    args = %W[
      --disable-silent-rules
      --sysconfdir=#{etc}
      --localstatedir=#{var}
    ]

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    assert_equal version.to_s, shell_output("pkgconf --modversion xorg-macros").chomp
  end
end
