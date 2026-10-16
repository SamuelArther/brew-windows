class Dcd < Formula
  desc "Auto-complete program for the D programming language"
  homepage "https://github.com/dlang-community/DCD"
  url "https://github.com/dlang-community/DCD/releases/download/v0.19.4/dcd-v0.19.4-windows-x86_64.zip"
  sha256 "0c5bf07c37f768b8f3f1756ddc7c2a31bc1cfc36ab36ee750eaea54dc6d4c663"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dlang-community/DCD.git"
  end

  def install
    bin.install "dcd-client.exe"
    bin.install "dcd-server.exe"
  end

  test do
    port = free_port

    # spawn a server, using a non-default port to avoid
    # clashes with pre-existing dcd-server instances
    server = spawn bin/"dcd-server", "-p", port.to_s
    # Give it generous time to load
    sleep 0.5
    # query the server from a client
    system bin/"dcd-client", "-q", "-p", port.to_s
  ensure
    Process.kill "TERM", server
    Process.wait server
  end
end
