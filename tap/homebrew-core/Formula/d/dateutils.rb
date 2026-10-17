class Dateutils < Formula
  desc "Tools to manipulate dates with a focus on financial data"
  homepage "https://www.fresse.org/dateutils/"
  url "https://github.com/hroptatyr/dateutils/releases/download/v0.4.12/dateutils-0.4.12.tar.xz"
  sha256 "1e0593116e1a229242255cf890f210cbe120e6f05e9d877faf8d85da675ade1a"
  license "BSD-3-Clause"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/dateutils-0.4.12"
    sha256 cellar: :any, x86_64_windows: "08b9cd0256e8007e6cdad9133279fa8cc9d02af221e4f014384c55d4d4edd499"
  end

  head do
    url "https://github.com/hroptatyr/dateutils.git", branch: "master"
    depends_on "autoconf" => :build
    depends_on "automake" => :build
    depends_on "libtool" => :build
  end

  def install
    system "autoreconf", "--force", "--install", "--verbose" if build.head?
    system "./configure", "--disable-silent-rules", *std_configure_args
    system "make", "install"
  end

  test do
    output = shell_output("#{bin}/dconv 2012-03-04 -f \"%Y-%m-%c-%w\"").strip
    assert_equal "2012-03-01-07", output
  end
end
