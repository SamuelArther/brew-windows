class Ltl2ba < Formula
  desc "Translate LTL formulae to Buchi automata"
  homepage "https://lsv.ens-paris-saclay.fr/~gastin/ltl2ba/"
  url "https://lsv.ens-paris-saclay.fr/~gastin/ltl2ba/ltl2ba-1.3.tar.gz"
  mirror "https://pkg.freebsd.org/ports-distfiles/ltl2ba-1.3.tar.gz"
  sha256 "912877cb2929cddeadfd545a467135a2c61c507bbd5ae0edb695f8b5af7ce9af"
  license "GPL-2.0-or-later"

  livecheck do
    url "https://lsv.ens-paris-saclay.fr/~gastin/ltl2ba/download.php", post_form: {
      getltl2ba: "Get LTL2BA",
    }
    regex(/href=.*?ltl2ba[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8ae5ce211d9cb142835b5457a7c42522b2fadda7cd1d49c5cf883690338e28c2"
  end

  def install
    system "make"
    bin.install "ltl2ba"
  end

  test do
    assert_match ":: (p) -> goto accept_all", shell_output("#{bin}/ltl2ba -f 'p if p ∈ w(0)'")
  end
end
