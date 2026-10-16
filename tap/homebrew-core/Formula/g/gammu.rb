class Gammu < Formula
  desc "Command-line utility to control a phone"
  homepage "https://wammu.eu/gammu/"
  url "https://github.com/gammu/gammu/releases/download/1.45.0/Gammu-1.45.0-Windows-Minimal.exe"
  sha256 "95ddc37189da53f6eadc4e1d8c57ba8abf985191b4cbe7aee7fd283a37882563"
  license "GPL-2.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gammu/gammu/releases/download/1.45.0/Gammu-1.45.0.tar.gz"
  end

  def install
    bin.install "bin/gammu.exe" => "gammu.exe"
  end

  test do
    system bin/"gammu", "--help"
  end
end
