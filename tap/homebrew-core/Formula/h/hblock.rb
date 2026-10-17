class Hblock < Formula
  desc "Adblocker that creates a hosts file from multiple sources"
  homepage "https://hblock.molinero.dev/"
  url "https://github.com/hectorm/hblock/archive/refs/tags/v3.5.1.tar.gz"
  sha256 "af98a6753e5de1406b63cd1fabf4b3eae84816168c532dae40c83092acb69941"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/hblock-3.5.1"
    sha256 cellar: :any, x86_64_windows: "3a376386f97d3ce2bcd99a0408bf6fc963eedefe36de00409028893215a2c0dd"
  end

  uses_from_macos "curl"

  def install
    system "make", "install", "prefix=#{prefix}", "bindir=#{bin}", "mandir=#{man}"
  end

  test do
    output = shell_output("#{bin}/hblock -H none -F none -S none -A none -D none -qO-")
    assert_match "Blocked domains:", output
  end
end
