class Topgrade < Formula
  desc "Upgrade all the things"
  homepage "https://github.com/topgrade-rs/topgrade"
  url "https://github.com/topgrade-rs/topgrade/releases/download/v17.12.3/topgrade-v17.12.3-x86_64-pc-windows-msvc.zip"
  sha256 "0cbfb8525eb997e37eb7b4ccd0b4c553d5bb27c34e2b1cb2762decf28b4f8347"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/topgrade-rs/topgrade/archive/refs/tags/v17.12.3.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "topgrade.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/topgrade --version 2>&1")
  end
end
