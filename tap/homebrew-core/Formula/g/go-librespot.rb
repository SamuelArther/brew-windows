class GoLibrespot < Formula
  desc "Spotify client"
  homepage "https://github.com/devgianlu/go-librespot"
  url "https://github.com/devgianlu/go-librespot/releases/download/v0.10.4/go-librespot_windows_amd64.tar.gz"
  sha256 "d15dbc60c09c2a5f03a4369ac4a3da5f4b67a40028fd6c25a96e5e1e4c72999f"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/devgianlu/go-librespot/archive/refs/tags/v0.10.4.tar.gz"
  end

  def install
    bin.install "go-librespot.exe"
  end

  test do
    assert_path_exists bin/"go-librespot.exe"
  end
end
