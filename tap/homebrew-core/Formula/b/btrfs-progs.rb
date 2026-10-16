class BtrfsProgs < Formula
  desc "Userspace utilities to manage btrfs filesystems"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "mkfs.btrfs", "mkfs.btrfs"
    windows_exe "libexec/wintools.exe", "btrfs", "btrfs"
  end

  def caveats
    <<~EOS
      mkfs.btrfs writes image files or whole disks (PhysicalDriveN devices, from an elevated prompt).
      Mounting btrfs on Windows is WinBtrfs' job (https://github.com/maharmstone/btrfs); commands that
      need a mounted file system (subvolume, balance, scrub, send/receive) go through it.
    EOS
  end

  test do
    device = testpath/"test.img"
    output = shell_output("#{bin}/mkfs.btrfs -b 128M #{device}")
    assert_match(/Filesystem size:\s*128\.00MiB/, output)
    output = shell_output("#{bin}/btrfs filesystem show #{device}")
    assert_match "Total devices 1 FS bytes used 112.00KiB", output
    assert_match "no error found", shell_output("#{bin}/btrfs check #{device}")
    system bin/"btrfs", "filesystem", "label", device, "brewtest"
    assert_equal "brewtest", shell_output("#{bin}/btrfs filesystem label #{device}").strip
    assert_match "Metadata, single", shell_output("#{bin}/btrfs filesystem df #{device}")
    assert_match "existing filesystem", shell_output("#{bin}/mkfs.btrfs #{device} 2>&1", 1)
  end
end
