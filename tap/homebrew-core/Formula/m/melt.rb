class Melt < Formula
  desc "Backup and restore Ed25519 SSH keys with seed words"
  homepage "https://github.com/charmbracelet/melt"
  url "https://github.com/charmbracelet/melt/releases/download/v0.6.2/melt_0.6.2_Windows_x86_64.zip"
  sha256 "26eeae944b9e50b1487fc2c7d86a6aaf57109346072d1574fcf2295fc9f4dd91"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/charmbracelet/melt/archive/refs/tags/v0.6.2.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/melt.exe", "melt"
  end

  test do
    output = shell_output("#{bin}/melt restore --seed \"seed\" ./restored_id25519 2>&1", 1)
    assert_match "Error: failed to get seed from mnemonic: Invalid mnenomic", output
  end
end
