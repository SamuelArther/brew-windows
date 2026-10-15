class Pvetui < Formula
  desc "Terminal UI for Proxmox VE"
  homepage "https://pvetui.org"
  url "https://github.com/devnullvoid/pvetui/releases/download/v1.4.4/pvetui_1.4.4_windows_amd64.zip"
  sha256 "f6f893f55348413e5e26032ffcf6359dadbaeb62f4d2c806b90e7b912dae8b24"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/devnullvoid/pvetui/releases/download/v1.4.4/pvetui_1.4.4_source.tar.gz"
  end

  def install
    bin.install "pvetui.exe"
  end

  test do
    assert_match "It looks like this is your first time running pvetui.", pipe_output(bin/"pvetui", "n")
    assert_match version.to_s, shell_output("#{bin}/pvetui --version")
  end
end
