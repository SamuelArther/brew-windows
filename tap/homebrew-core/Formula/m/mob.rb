class Mob < Formula
  desc "Tool for smooth Git handover in mob programming sessions"
  homepage "https://mob.sh"
  url "https://github.com/remotemobprogramming/mob/releases/download/5.4.2/mob_5.4.2_windows_amd64.tar.gz"
  sha256 "5ba31bf1c0885cab5782d72d9fce1abf1190dc2048873c5c57fd4baf71bd576f"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/remotemobprogramming/mob/archive/refs/tags/5.4.2.tar.gz"
  end

  def install
    bin.install "mob.exe"
  end

  test do
    assert_path_exists bin/"mob.exe"
  end
end
