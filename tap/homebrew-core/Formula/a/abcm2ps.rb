class Abcm2ps < Formula
  desc "ABC music notation software"
  homepage "http://moinejf.free.fr"
  url "https://chiselapp.com/user/moinejf/repository/abcm2ps/tarball/v8.14.18/download.tar.gz"
  sha256 "d1f1100b525f0f0ae00d706d0b4ebc01df279312b3b32cf20f355f1430f36c0a"
  license "GPL-3.0-or-later"

  livecheck do
    url "https://chiselapp.com/user/moinejf/repository/abcm2ps/taglist"
    regex(%r{"tagDsp">v?(\d+(?:\.\d+)+)</span>}i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "29557ae6dc48ce6ea98f0465686fdb98b6f9a0c4eabdeedc7678c3c699fc4f68"
  end

  depends_on "pkgconf" => :build

  on_macos do
    depends_on "coreutils" => :build
    depends_on "gnu-sed" => :build
  end

  deny_network_access!

  def install
    if OS.mac?
      ENV.prepend_path "PATH", formula_opt_libexec("gnu-sed")/"gnubin"
      ENV.prepend_path "PATH", formula_opt_libexec("coreutils")/"gnubin"
    end

    system "./configure", "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    (testpath/"voices.abc").write <<~ABC
      X:7
      T:Qui Tolis (Trio)
      C:Andre Raison
      M:3/4
      L:1/4
      Q:1/4=92
      %%staves {(Pos1 Pos2) Trompette}
      K:F
      %
      V:Pos1
      %%MIDI program 78
      "Positif"x3 |x3|c'>ba|Pga/g/f|:g2a |ba2 |g2c- |c2P=B  |c>de  |fga |
      V:Pos2
      %%MIDI program 78
              Mf>ed|cd/c/B|PA2d |ef/e/d |:e2f |ef2 |c>BA |GA/G/F |E>FG |ABc- |
      V:Trompette
      %%MIDI program 56
      "Trompette"z3|z3 |z3 |z3 |:Mc>BA|PGA/G/F|PE>EF|PEF/E/D|C>CPB,|A,G,F,-|
    ABC

    system bin/"abcm2ps", testpath/"voices"
    assert_path_exists testpath/"Out.ps"
  end
end
