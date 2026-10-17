class Sendemail < Formula
  desc "Email program for sending SMTP mail"
  homepage "http://caspian.dotconf.net/menu/Software/SendEmail/"
  url "https://deb.debian.org/debian/pool/main/s/sendemail/sendemail_1.56.orig.tar.gz"
  mirror "http://caspian.dotconf.net/menu/Software/SendEmail/sendEmail-v1.56.tar.gz"
  sha256 "6dd7ef60338e3a26a5e5246f45aa001054e8fc984e48202e4b0698e571451ac0"
  license "GPL-2.0-or-later"

  # Upstream homepage is gone
  deprecate! date: "2026-01-05", because: :repo_removed
  disable! date: "2027-01-05", because: :repo_removed

  # Reported upstream: https://web.archive.org/web/20191013154932/caspian.dotconf.net/menu/Software/SendEmail/#comment-1119965648
  patch do
    url "https://raw.githubusercontent.com/mogaal/sendemail/e785a6d284884688322c9b39c0f64e20a43ea825/debian/patches/fix_ssl_version.patch"
    sha256 "0b212ade1808ff51d2c6ded5dc33b571f951bd38c1348387546c0cdf6190c0c3"
    type :unofficial
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "644b5a61c22d2a4f6ba932ed1eeab564b2b032a7d0e504b11772d1f44c729c8b"
  end

  def install
    bin.install "sendEmail"
  end

  test do
    assert_match "sendEmail-#{version}", shell_output("#{bin}/sendEmail", 1).strip
  end
end
