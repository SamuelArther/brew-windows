class Hello < Formula
  desc "Program providing model for GNU coding standards and practices"
  homepage "https://www.gnu.org/software/hello/"
  url "https://ftpmirror.gnu.org/hello/hello-2.12.3.tar.gz"
  sha256 "0d5f60154382fee10b114a1c34e785d8b1f492073ae2d3a6f7b147687b366aa0"
  license "GPL-3.0-or-later"

  deny_network_access!

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/hello-2.12.3"
    sha256 cellar: :any, x86_64_windows: "ffeb7343cd2cf63302341b563f44dbdb05cfa10b4948495553b86cee47941a8a"
  end

  def install
    ENV.append "LDFLAGS", "-liconv" if OS.mac?

    system "./configure", "--disable-dependency-tracking", *std_configure_args
    system "make", "install"
  end

  test do
    assert_equal "brew", shell_output("#{bin}/hello --greeting=brew").chomp
  end
end
