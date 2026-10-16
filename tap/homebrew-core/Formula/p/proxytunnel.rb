class Proxytunnel < Formula
  desc "Create TCP tunnels through HTTPS proxies"
  homepage "https://github.com/proxytunnel/proxytunnel"
  url "https://github.com/proxytunnel/proxytunnel/releases/download/v1.13.0/proxytunnel-v1.13.0-x86_64-windows-msys.zip"
  sha256 "e2d5535fdcc2348de5de33f8057508da2da947a3916f4a993b3eea3cb9d91272"
  license "GPL-2.0-or-later" => { with: "x11vnc-openssl-exception" }

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/proxytunnel/proxytunnel/archive/refs/tags/v1.13.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/proxytunnel.exe", "proxytunnel"
  end

  test do
    system bin/"proxytunnel", "--version"
  end
end
