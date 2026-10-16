class TkeySshAgent < Formula
  desc "SSH agent for use with the TKey security stick"
  homepage "https://tillitis.se/"
  url "https://github.com/tillitis/tkey-ssh-agent/releases/download/v1.1.1/tkey-ssh-agent_1.1.1_Windows_amd64.zip"
  sha256 "42d582cdcb3f7d40039a82c61cccd044e72236587b5f46b4db04036d3d1b610b"
  license "GPL-2.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tillitis/tkey-ssh-agent/archive/refs/tags/v1.1.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    windows_exe "libexec/tkey-ssh-agent.exe", "tkey-ssh-agent"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tkey-ssh-agent --version 2>&1")
  end
end
