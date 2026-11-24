class Curseofwar < Formula
  desc "Fast-paced action strategy game"
  homepage "https://a-nikolaev.github.io/curseofwar/"
  url "https://github.com/a-nikolaev/curseofwar/archive/refs/tags/v1.3.0.tar.gz"
  sha256 "2a90204d95a9f29a0e5923f43e65188209dc8be9d9eb93576404e3f79b8a652b"
  license "GPL-3.0-or-later"
  head "https://github.com/a-nikolaev/curseofwar.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3a2d11ee5c0475d638d8da76fa14ebdf7fe622e743cd3db0f3bcfbde6cd7fa14"
  end

  uses_from_macos "ncurses"

  deny_network_access!

  def install
    system "make", "VERSION=#{version}"
    bin.install "curseofwar"
    man6.install "curseofwar.6"
  end

  test do
    assert_equal version.to_s, shell_output("#{bin}/curseofwar -v", 1).chomp
  end
end
