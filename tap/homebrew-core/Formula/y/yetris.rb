class Yetris < Formula
  desc "Customizable Tetris for the terminal"
  homepage "https://github.com/alexdantas/yetris"
  url "https://github.com/alexdantas/yetris/archive/refs/tags/v2.3.0.tar.gz"
  sha256 "720c222325361e855e2dcfec34f8f0ae61dd418867a87f7af03c9a59d723b919"
  license "GPL-3.0-or-later"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/yetris-2.3.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "70bd703660671c4ac174e25b8b718af364f574a3a0f8adc33fb4e2876d1ec60a"
  end

  uses_from_macos "ncurses"

  def install
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    assert_match "v#{version}", shell_output("#{bin}/yetris --version")
  end
end
