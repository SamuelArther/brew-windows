class OpeniothubServer < Formula
  desc "Server for OpenIoTHub"
  homepage "https://github.com/OpenIoTHub/server-go"
  url "https://github.com/OpenIoTHub/server-go/releases/download/v1.2.25/server-go_1.2.25_windows_amd64.tar.gz"
  sha256 "575f99fa910fa81aa3c6ac6a59235df853ba4f16a06f9b8041db7ce350a0a347"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/OpenIoTHub/server-go.git"
  end

  def install
    bin.install "server-go.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/server-go --version 2>&1")
  end
end
