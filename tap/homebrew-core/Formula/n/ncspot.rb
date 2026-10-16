class Ncspot < Formula
  desc "Cross-platform ncurses Spotify client written in Rust"
  homepage "https://github.com/hrkfdn/ncspot"
  url "https://github.com/hrkfdn/ncspot/releases/download/v1.5.0/ncspot-v1.5.0-windows-x86_64.zip"
  sha256 "547e5bae5293d89144e27968c49071d5b36da8dc7200193c0214ea2180f7c4ed"
  license "BSD-2-Clause"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/hrkfdn/ncspot/archive/refs/tags/v1.5.0.tar.gz"
  end

  def install
    bin.install "ncspot.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ncspot --version 2>&1")
  end
end
