class Esniper < Formula
  desc "Snipe eBay auctions from the command-line"
  homepage "https://sourceforge.net/projects/esniper/"
  url "https://downloads.sourceforge.net/project/esniper/esniper/2.35.0/esniper-2-35-0.tgz"
  version "2.35.0"
  sha256 "a93d4533e31640554f2e430ac76b43e73a50ed6d721511066020712ac8923c12"
  license "BSD-2-Clause"

  no_autobump! because: :incompatible_version_format

  # https://sourceforge.net/p/esniper/git/ci/e2b495c6aae6596a2c8ac039d76afdf0764b06d7/
  deprecate! date: "2026-03-30", because: :unmaintained
  disable! date: "2026-09-30", because: :unmaintained

  uses_from_macos "curl"

  deny_network_access!

  def install
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    assert_match "Auction file \"test.auction\" is not readable", shell_output("#{bin}/esniper test.auction 2>&1", 1)
  end
end
