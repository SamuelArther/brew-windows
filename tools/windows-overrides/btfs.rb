class Btfs < Formula
  desc "BitTorrent filesystem on the Windows Projected File System"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "btfs", "btfs"
  end

  def caveats
    <<~EOS
      btfs needs the Windows Projected File System. Turn it on once (as admin) with:
        Enable-WindowsOptionalFeature -Online -FeatureName Client-ProjFS -NoRestart
      Then `btfs file.torrent mnt` (or a magnet link with tr= trackers) and unmount with `btfs -u mnt`.
    EOS
  end

  test do
    system bin/"btfs", "--help"
    assert_match "btfs version", shell_output("#{bin}/btfs --version")
    (testpath/"bad.torrent").write "not a torrent"
    output = shell_output("#{bin}/btfs #{testpath}/bad.torrent #{testpath}/mnt 2>&1", 1)
    assert_match "Failed to parse metadata", output
    assert_match "info hash", shell_output("#{bin}/btfs 'magnet:?dn=nothing' #{testpath}/mnt 2>&1", 1)
  end
end
