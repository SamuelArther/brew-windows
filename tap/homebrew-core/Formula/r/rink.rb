class Rink < Formula
  desc "Unit conversion tool and library written in rust"
  homepage "https://rinkcalc.app/about"
  url "https://github.com/tiffany352/rink-rs/releases/download/v0.9.0/Rink.CLI.Windows.zip"
  sha256 "9684e738fa169b1f0de88e8b351fb1c2c870986e955276725cf9a9c8f28993ce"
  license all_of: ["MPL-2.0", "GPL-3.0-or-later"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tiffany352/rink-rs/archive/refs/tags/v0.9.0.tar.gz"
  end

  def install
    bin.install "rink.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rink --version 2>&1")
  end
end
