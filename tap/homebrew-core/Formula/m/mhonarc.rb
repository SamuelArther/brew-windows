class Mhonarc < Formula
  desc "Mail-to-HTML converter"
  homepage "https://www.mhonarc.org/"
  url "https://cpan.metacpan.org/authors/id/L/LD/LDIDRY/MHonArc-2.6.24.tar.gz"
  sha256 "457dc7374ee59cb75a0729e51cef2f2c52b48180f739d8fd956ea19882815f33"
  license "GPL-2.0-or-later"
  revision 2
  head "https://github.com/sympa-community/mhonarc.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "818863530d36f5f394d7d02dd2b79374c607657633ecd279fcfd8e9cfbaff663"
  end

  depends_on "perl"

  def install
    # Using Perl's `installprefix` rather than `prefix` allows install.me to use
    # Homebrew Perl directory structure even if the prefixes are different paths.
    inreplace "install.me", "$Config{'prefix'}", "$Config{'installprefix'}"

    system "perl", "install.me",
           "-batch",
           "-perl", formula_opt_bin("perl")/"perl",
           "-prefix", prefix

    bin.install "mhonarc"
  end

  test do
    system bin/"mhonarc", "-v"
  end
end
