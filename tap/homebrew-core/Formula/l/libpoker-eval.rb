class LibpokerEval < Formula
  desc "C library to evaluate poker hands"
  homepage "https://pokersource.sourceforge.net/"
  # http://download.gna.org/pokersource/sources/poker-eval-138.0.tar.gz is offline
  url "https://launchpad.net/ubuntu/+archive/primary/+sourcefiles/poker-eval/138.0-1/poker-eval_138.0.orig.tar.gz"
  sha256 "92659e4a90f6856ebd768bad942e9894bd70122dab56f3b23dd2c4c61bdbcf68"
  license "GPL-3.0-or-later"

  livecheck do
    skip "No longer developed or maintained"
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/libpoker-eval-138.0"
    sha256 cellar: :any, x86_64_windows: "17a1300cd46bd4b585fc165998c1a6e21783e6c5819451f286a62a95cb6dec9d"
  end

  depends_on "pkgconf" => :test

  deny_network_access!

  def install
    args = []
    # Help old config scripts identify arm64 linux
    args << "--build=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm? && Hardware::CPU.is_64_bit?

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <poker_defs.h>
      #include <inlines/eval.h>

      int main(void) {
        char *cards[] = { "Ah", "Kh", "Qh", "Jh", "Th" };
        CardMask hand;
        int i, card;

        CardMask_RESET(hand);
        for (i = 0; i < 5; i++) {
          if (StdDeck_stringToCard(cards[i], &card) == 0) return 1;
          StdDeck_CardMask_SET(hand, card);
        }
        StdRules_HandVal_print(StdDeck_StdRules_EVAL_N(hand, 5));
        return 0;
      }
    C

    pkgconf_flags = shell_output("pkgconf --cflags --libs poker-eval").chomp.split
    system ENV.cc, "test.c", "-o", "test", *pkgconf_flags
    assert_equal "StFlush (A)", shell_output("./test").strip
  end
end
