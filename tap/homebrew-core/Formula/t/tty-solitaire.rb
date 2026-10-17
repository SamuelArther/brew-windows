class TtySolitaire < Formula
  desc "Ncurses-based klondike solitaire game"
  homepage "https://github.com/mpereira/tty-solitaire"
  url "https://github.com/mpereira/tty-solitaire/archive/refs/tags/v1.4.1.tar.gz"
  sha256 "8eb536a87f0586e1f057a3aa05256df34cbd92a3e22545d1df2f945e27d89db2"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "57be6a152059cac7bde94539cdd9eebd9e4b2abfde13736d2c90e64187a24c8e"
  end

  uses_from_macos "ncurses"

  def install
    system "make"
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    system bin/"ttysolitaire", "-h"
  end
end
