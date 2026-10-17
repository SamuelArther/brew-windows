class Lavat < Formula
  desc "Lava lamp simulation using metaballs in the terminal"
  homepage "https://github.com/AngelJumbo/lavat"
  url "https://github.com/AngelJumbo/lavat/archive/refs/tags/v3.0.0.tar.gz"
  sha256 "c5364203a75395953560b173fae90c316b753a046acb8f557c9e684eec6d76ba"
  license "MIT"
  head "https://github.com/AngelJumbo/lavat.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "ae90939170fdc597fe362c76f8f22f1bdcde7877a0bc871a00dc5772b7a3e7a7"
  end

  def install
    system "make", "PREFIX=#{prefix}", "install"
  end

  test do
    # GUI app
    assert_match "Usage: lavat [OPTIONS]", shell_output("#{bin}/lavat -h")

    require "pty"

    PTY.spawn(bin/"lavat") do |_r, _w, pid|
      sleep 5
    ensure
      Process.kill("TERM", pid)
    end
  end
end
