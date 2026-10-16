class FuseOverlayfs < Formula
  desc "Overlay file system on the Windows Projected File System"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "fuse-overlayfs", "fuse-overlayfs"
  end

  def caveats
    <<~EOS
      fuse-overlayfs needs the Windows Projected File System. Turn it on once (as admin) with:
        Enable-WindowsOptionalFeature -Online -FeatureName Client-ProjFS -NoRestart
      Unmount with: fuse-overlayfs -u <mountpoint>
    EOS
  end

  test do
    assert_match "version 1.18", shell_output("#{bin}/fuse-overlayfs --version")
    mkdir "lowerdir/a"
    mkdir "up"
    mkdir "merged"
    output = shell_output("#{bin}/fuse-overlayfs -o lowerdir=lowerdir/a:lowerdir/missing,upperdir=up,workdir=workdir merged 2>&1", 1)
    assert_match "cannot open lowerdir", output
    output = shell_output("#{bin}/fuse-overlayfs -o lowerdir=lowerdir/a,upperdir=up merged 2>&1", 1)
    assert_match "workdir not specified", output
    assert_match "not mounted", shell_output("#{bin}/fuse-overlayfs -u merged 2>&1", 1)
  end
end
