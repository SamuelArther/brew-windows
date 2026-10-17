class Nbsdgames < Formula
  desc "Text-based modern games"
  homepage "https://github.com/abakh/nbsdgames"
  url "https://github.com/abakh/nbsdgames/archive/refs/tags/v6.0.3.tar.gz"
  sha256 "359da5f698da00437205eddad3fc97fbdcecfa8cb005fd8d1830fe8fd3dd7e3b"
  license :public_domain
  head "https://github.com/abakh/nbsdgames.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "188520774d983a93fd4a5cc6c21da8d8245ff0f0d1a0f9be899da05582d30509"
  end

  depends_on "pkgconf" => :build

  uses_from_macos "ncurses"

  def install
    mkdir bin
    system "make", "install",
           "GAMES_DIR=#{bin}",
           "SCORES_DIR=#{var}/games",
           "MAN_DIR=#{man}",
           "LIBS_PKG_CONFIG=-lncurses"

    man6.mkpath
    system "make", "manpages", "MAN_DIR=#{man6}"
  end

  test do
    assert_equal "2 <= size <= 7", shell_output("#{bin}/sudoku -s 1", 1).chomp
  end
end
