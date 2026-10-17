class Shmux < Formula
  desc "Execute the same command on many hosts in parallel"
  homepage "https://github.com/shmux/shmux"
  url "https://github.com/shmux/shmux/archive/refs/tags/v1.0.3.tar.gz"
  sha256 "c9f8863e2550e23e633cf5fc7a9c4c52d287059f424ef78aba6ecd98390fb9ab"
  license "BSD-3-Clause"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/shmux-1.0.3"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "24d05dde6941d504be7d4418f33d6bcb5cd292dc7b10f21e68c5548da8224898"
  end

  uses_from_macos "ncurses"

  def install
    system "./configure", "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    system bin/"shmux", "-h"
  end
end
