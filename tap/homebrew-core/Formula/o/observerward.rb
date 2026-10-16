class Observerward < Formula
  desc "Web application and service fingerprint identification tool"
  homepage "https://blog.kali-team.cn/projects/observer_ward/"
  url "https://github.com/emo-crab/observer_ward/releases/download/v2026.6.28/observer_ward_v2026.6.28_x86_64-pc-windows-gnu.zip"
  sha256 "7ad77a9ec4c36982b999c23b74f1509ec4bd899f440b0d37617c2578b2744fe5"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/emo-crab/observer_ward/archive/refs/tags/v2026.6.28.tar.gz"
  end

  def install
    bin.install "x86_64-pc-windows-gnu.exe" => "observer_ward.exe"
  end

  test do
    system bin/"observer_ward", "--help"
  end
end
