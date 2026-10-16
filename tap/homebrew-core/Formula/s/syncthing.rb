class Syncthing < Formula
  desc "Open source continuous file synchronization application"
  homepage "https://syncthing.net/"
  url "https://github.com/syncthing/syncthing/releases/download/v2.1.6/syncthing-windows-amd64-v2.1.6.zip"
  sha256 "589098269cabb9a5a1703a934305b4153609d7623e978e1c94f0f49d77ae5e9f"
  license "MPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/syncthing/syncthing/archive/refs/tags/v2.1.6.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/syncthing.exe", "syncthing"
  end

  test do
    assert_match "syncthing v#{version} ", shell_output("#{bin}/syncthing version")
    system bin/"syncthing", "generate"
  end
end
