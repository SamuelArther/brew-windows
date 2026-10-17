class Nsnake < Formula
  desc "Classic snake game with textual interface"
  homepage "https://github.com/alexdantas/nSnake"
  url "https://downloads.sourceforge.net/project/nsnake/GNU-Linux/nsnake-3.0.1.tar.gz"
  sha256 "e0a39e0e188a6a8502cb9fc05de3fa83dd4d61072c5b93a182136d1bccd39bb9"
  license "GPL-3.0-or-later"
  head "https://github.com/alexdantas/nSnake.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "70d9e9ff669f623284c023032a6da65a4bc475ab6e562db7f4d9e5996c40875a"
  end

  uses_from_macos "ncurses"

  def install
    system "make", "install", "PREFIX=#{prefix}"

    # No need for Linux desktop
    rm_r(share/"applications")
    rm_r(share/"icons")
    rm_r(share/"pixmaps")
  end

  test do
    assert_match "nsnake v#{version} ", shell_output("#{bin}/nsnake -v")
  end
end
