class Rsyncy < Formula
  desc "Status/progress bar for rsync"
  homepage "https://github.com/laktak/rsyncy"
  url "https://github.com/laktak/rsyncy/releases/download/v2.2.0/rsyncy-windows-amd64.zip"
  sha256 "45eceb868fa63277bccbc4e86423814c2efe5e5c1907f815ddab6249e1531683"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/laktak/rsyncy/archive/refs/tags/v2.2.0.tar.gz"
  end

  def install
    bin.install "rsyncy.exe"
  end

  test do
    assert_path_exists bin/"rsyncy.exe"
  end
end
